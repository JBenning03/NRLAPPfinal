using System.Security.Claims;
using System.Text.Json;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using NRLApp.Models;

namespace NRLApp.Controllers
{
    // INGEN [Authorize] PÅ KLASSE-NIVÅ – alle får se som default
    public class ObstacleController : Controller
    {
        private readonly IConfiguration _config;
        public ObstacleController(IConfiguration config) => _config = config;

        private MySqlConnection CreateConnection()
            => new MySqlConnection(_config.GetConnectionString("DefaultConnection"));

        // Holder geometri mellom steg (lagres i TempData-cookie)
        [TempData] public string? DrawJson { get; set; }

        private DrawState GetDrawState()
            => string.IsNullOrWhiteSpace(DrawJson)
                ? new DrawState()
                : (JsonSerializer.Deserialize<DrawState>(DrawJson!) ?? new DrawState());

        private void SaveDrawState(DrawState s)
            => DrawJson = JsonSerializer.Serialize(s);

        // ===== STEP 1: Tegn markør/linje/område =====
        [HttpGet]
        public IActionResult Area() => View();

        // Tar imot GeoJSON fra skjema
        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Area(string geoJson)
        {
            if (string.IsNullOrWhiteSpace(geoJson))
            {
                TempData["Error"] = "Du må plassere en markør, tegne en linje eller et område.";
                return RedirectToAction(nameof(Area));
            }

            SaveDrawState(new DrawState { GeoJson = geoJson });

            return RedirectToAction(nameof(Meta));
        }

        // ===== STEP 2: Skriv inn metadata =====
        [HttpGet]
        public IActionResult Meta()
        {
            var s = GetDrawState();

            if (string.IsNullOrWhiteSpace(s.GeoJson))
                return RedirectToAction(nameof(Area));

            TempData.Keep(nameof(DrawJson));

            return View(new ObstacleMetaVm());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Meta(ObstacleMetaVm vm, string? action)
        {
            var s = GetDrawState();
            var geoJsonToSave = string.IsNullOrWhiteSpace(s.GeoJson) ? "{}" : s.GeoJson;

            if (string.IsNullOrWhiteSpace(vm.ObstacleName))
                ModelState.AddModelError(nameof(vm.ObstacleName), "Skriv hva det er.");
            if (vm.HeightValue is null || vm.HeightValue < 0)
                ModelState.AddModelError(nameof(vm.HeightValue), "Oppgi høyde.");

            if (!ModelState.IsValid)
                return View(vm);

            double heightMeters = vm.HeightValue!.Value;

            if (string.Equals(vm.HeightUnit, "ft", StringComparison.OrdinalIgnoreCase))
                heightMeters = Math.Round(heightMeters * 0.3048, 0);

            bool isDraft = string.Equals(action, "draft", StringComparison.OrdinalIgnoreCase) || vm.SaveAsDraft;

            const string sql = @"
INSERT INTO obstacles (geojson, obstacle_name, height_m, obstacle_description, is_draft, created_utc, created_by_user_id, review_status)
VALUES (@GeoJson, @Name, @HeightM, @Descr, @IsDraft, UTC_TIMESTAMP(), @CreatedBy, @ReviewStatus);";

            using var con = CreateConnection();

            try
            {
                await con.ExecuteAsync(sql, new
                {
                    GeoJson = geoJsonToSave,
                    Name = vm.ObstacleName,
                    HeightM = (int?)Math.Round(heightMeters, 0),
                    Descr = vm.Description,
                    IsDraft = isDraft ? 1 : 0,
                    CreatedBy = User?.Identity?.IsAuthenticated == true
                        ? User.FindFirstValue(ClaimTypes.NameIdentifier)
                        : null,
                    ReviewStatus = isDraft ? null : ObstacleStatus.Pending.ToString()
                });
            }
            catch
            {
                // logging hvis ønskelig
            }

            DrawJson = null;
            return RedirectToAction(nameof(Thanks), new { draft = isDraft });
        }

        // ===== Takk =====
        [HttpGet]
        public IActionResult Thanks(bool draft = false)
        {
            ViewBag.Draft = draft;
            return View();
        }

        // ===== Liste – ÅPEN FOR ALLE =====
        [HttpGet]
        public async Task<IActionResult> List()
        {
            using var con = CreateConnection();
            const string sql = @"
SELECT o.id,
       o.obstacle_name        AS ObstacleName,
       o.height_m             AS HeightMeters,
       o.is_draft             AS IsDraft,
       o.created_utc          AS CreatedUtc,
       o.review_status        AS ReviewStatus,
       o.review_comment       AS ReviewComment,
       createdBy.UserName     AS CreatedByUserName,
       assignedTo.UserName    AS AssignedToUserName
FROM obstacles o
LEFT JOIN AspNetUsers createdBy ON createdBy.Id = o.created_by_user_id
LEFT JOIN AspNetUsers assignedTo ON assignedTo.Id = o.assigned_to_user_id
ORDER BY o.id DESC;";

            var rows = await con.QueryAsync<ObstacleListItem>(sql);
            return View(rows);
        }

        // ===== Vis – OGSÅ ÅPEN FOR ALLE =====
        [HttpGet]
        public async Task<IActionResult> Vis(int id)
        {
            using var con = CreateConnection();

            const string sql = @"
SELECT o.id,
       o.obstacle_name        AS ObstacleName,
       o.height_m             AS HeightMeters,
       o.obstacle_description AS Description,
       o.geojson              AS GeoJson,
       o.is_draft             AS IsDraft,
       o.created_utc          AS CreatedUtc,
       o.review_status        AS ReviewStatus,
       o.review_comment       AS ReviewComment,
       createdBy.UserName     AS CreatedByUserName,
       assignedTo.UserName    AS AssignedToUserName
FROM obstacles o
LEFT JOIN AspNetUsers createdBy ON createdBy.Id = o.created_by_user_id
LEFT JOIN AspNetUsers assignedTo ON assignedTo.Id = o.assigned_to_user_id
WHERE o.id = @Id;";

            var obstacle = await con.QuerySingleOrDefaultAsync<ObstacleDetailsVm>(sql, new { Id = id });

            if (obstacle == null)
                return NotFound();

            return View(obstacle);
        }

        // ===== Godkjenn / Avvis – KUN ADMIN/APPROVER =====
        [HttpPost]
        [Authorize(Roles = "Admin,Approver")]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> Approve(int id, string? reviewComment)
            => UpdateReviewStatus(id, ObstacleStatus.Approved, reviewComment);

        [HttpPost]
        [Authorize(Roles = "Admin,Approver")]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> Reject(int id, string? reviewComment)
            => UpdateReviewStatus(id, ObstacleStatus.Rejected, reviewComment);

        [Authorize(Roles = "Admin,Approver")]
        private async Task<IActionResult> UpdateReviewStatus(int id, ObstacleStatus status, string? reviewComment)
        {
            var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Challenge();

            using var con = CreateConnection();
            const string sql = @"
UPDATE obstacles
SET review_status = @Status,
    review_comment = @Comment,
    assigned_to_user_id = @AssignedTo
WHERE id = @Id;";

            var rows = await con.ExecuteAsync(sql, new
            {
                Status = status.ToString(),
                Comment = string.IsNullOrWhiteSpace(reviewComment)
                    ? null
                    : reviewComment.Trim(),
                AssignedTo = userId,
                Id = id
            });

            if (rows == 0)
                return NotFound();

            TempData["StatusMessage"] = status == ObstacleStatus.Approved
                ? "Hinderet er godkjent."
                : "Hinderet er avvist.";

            return RedirectToAction(nameof(Vis), new { id });
        }
    }
}
