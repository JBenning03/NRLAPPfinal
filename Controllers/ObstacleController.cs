using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using NRLApp.Models;
using NRLApp.Models.Obstacles;

namespace NRLApp.Controllers
{
    [Authorize]
    public class ObstacleController : Controller
    {
        private readonly IConfiguration _config;
        public ObstacleController(IConfiguration config) => _config = config;

        // Oppretter MYSQL-tilkobling
        private MySqlConnection CreateConnection()
            => new MySqlConnection(_config.GetConnectionString("DefaultConnection"));

        // TempData lagrer GeoJSON mellom requests (cookie-basert)
        [TempData] public string? DrawJson { get; set; }

        // Leser lagret GeoJSON
        private DrawState GetDrawState()
            => string.IsNullOrWhiteSpace(DrawJson)
                ? new DrawState()
                : (System.Text.Json.JsonSerializer.Deserialize<DrawState>(DrawJson!) ?? new DrawState());

        // Skriver til TempData
        private void SaveDrawState(DrawState s)
            => DrawJson = System.Text.Json.JsonSerializer.Serialize(s);

        // =========================================================
        // 1) TEGN OMRÅDE (AREA) – Første steg
        // =========================================================
        private bool IsAdmin() => User.IsInRole("Admin");

        [HttpGet]
        public IActionResult Area()
        {
            if (IsAdmin())
                return RedirectToAction("Users", "Admin");

            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Area(string geoJson)
        {
            if (IsAdmin())
                return RedirectToAction("Users", "Admin");

            // Må ha GeoJSON, ellers gi feilmelding
            if (string.IsNullOrWhiteSpace(geoJson))
            {
                TempData["Error"] = "Du må plassere en markør, tegne en linje eller et område.";
                return RedirectToAction(nameof(Area));
            }

            // Lagre valgt geometri i TempData
            SaveDrawState(new DrawState { GeoJson = geoJson });

            // Gå videre til metadata-skjema
            return RedirectToAction(nameof(Meta));
        }

        // =========================================================
        // 2) METADATA – Navn, høyde, beskrivelse osv.
        // =========================================================

        [HttpGet]
        public IActionResult Meta()
        {
            var s = GetDrawState();

            // Bruker forsøker å gå direkte inn på Meta uten å tegne noe først
            if (string.IsNullOrWhiteSpace(s.GeoJson))
                return RedirectToAction(nameof(Area));

            TempData.Keep(nameof(DrawJson)); // Bevar data videre

            return View(new ObstacleMetaVm());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Meta(ObstacleMetaVm vm, string? action)
        {
            var s = GetDrawState();
            var geoJsonToSave = string.IsNullOrWhiteSpace(s.GeoJson) ? "{}" : s.GeoJson;

            // Validering

            // nå krever vi kategori i stedet for hinder-tekst
            if (string.IsNullOrWhiteSpace(vm.Category))
                ModelState.AddModelError(nameof(vm.Category), "Velg kategori.");

            if (vm.HeightValue is null || vm.HeightValue < 0)
                ModelState.AddModelError(nameof(vm.HeightValue), "Oppgi høyde.");

            if (!ModelState.IsValid)
                return View(vm);

            // Konverter høyde hvis bruker har valgt ft
            double heightMeters = vm.HeightValue!.Value;
            if (string.Equals(vm.HeightUnit, "ft", StringComparison.OrdinalIgnoreCase))
                heightMeters = Math.Round(heightMeters * 0.3048, 0);

            bool isDraft = string.Equals(action, "draft", StringComparison.OrdinalIgnoreCase) || vm.SaveAsDraft;

            // Hent bruker-ID
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            const string sql = @"
INSERT INTO obstacles (
    geojson,
    obstacle_name,
    obstacle_category,
    height_m,
    obstacle_description,
    is_draft,
    created_utc,
    created_by_user_id
)
VALUES (
    @GeoJson,
    @Name,
    @Category,
    @HeightM,
    @Descr,
    @IsDraft,
    UTC_TIMESTAMP(),
    @CreatedByUserId
);";

            using var con = CreateConnection();
            await con.ExecuteAsync(sql, new
            {
                GeoJson = geoJsonToSave,
                Name = string.IsNullOrWhiteSpace(vm.ObstacleName)
         ? vm.Category
         : vm.ObstacleName,
                Category = vm.Category,
                HeightM = (int?)Math.Round(heightMeters, 0),
                Descr = vm.Description,
                IsDraft = isDraft ? 1 : 0,
                CreatedByUserId = userId
            });

            DrawJson = null;

            return RedirectToAction(nameof(Thanks), new { draft = isDraft });
        }

        // =========================================================
        // 3) TAKKESIDE – Vis hva som skjedde
        // =========================================================

        [HttpGet]
        public IActionResult Thanks(bool draft = false)
        {
            ViewBag.Draft = draft;
            return View();
        }

        // =========================================================
        // 4) LISTE / FILTRERING – Viser hindere i tabell
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] ObstacleListFilter filter)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Admin og Approver skal se alle.
            // Pilot / Crew (og andre) skal bare se egne innmeldinger.
            var isReviewer = User.IsInRole("Admin") || User.IsInRole("Approver");

            using var con = CreateConnection();
            var where = new List<string>();
            var parameters = new DynamicParameters();

            // Begrens til egne hindringer hvis man ikke er reviewer
            if (!isReviewer && !string.IsNullOrWhiteSpace(userId))
            {
                where.Add("o.created_by_user_id = @UserId");
                parameters.Add("UserId", userId);
            }

            if (filter.Id.HasValue)
            {
                where.Add("o.id = @Id");
                parameters.Add("Id", filter.Id.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.ObstacleName))
            {
                where.Add("LOWER(o.obstacle_name) LIKE @ObstacleName");
                parameters.Add("ObstacleName", $"%{filter.ObstacleName.Trim().ToLowerInvariant()}%");
            }

            if (filter.MinHeightMeters.HasValue)
            {
                where.Add("o.height_m >= @MinHeight");
                parameters.Add("MinHeight", filter.MinHeightMeters.Value);
            }

            if (filter.MaxHeightMeters.HasValue)
            {
                where.Add("o.height_m <= @MaxHeight");
                parameters.Add("MaxHeight", filter.MaxHeightMeters.Value);
            }

            if (filter.Status.HasValue)
            {
                where.Add("o.is_draft = @IsDraft");
                parameters.Add("IsDraft",
                    filter.Status.Value == ObstacleListStatusFilter.Draft ? 1 : 0);
            }

            var createdFromUtc = NormalizeToUtc(filter.CreatedFrom);
            if (createdFromUtc.HasValue)
            {
                where.Add("o.created_utc >= @CreatedFrom");
                parameters.Add("CreatedFrom", createdFromUtc.Value);
            }

            var createdToUtc = NormalizeToUtc(filter.CreatedTo);
            if (createdToUtc.HasValue)
            {
                where.Add("o.created_utc <= @CreatedTo");
                parameters.Add("CreatedTo", createdToUtc.Value);
            }

            var whereClause = where.Count == 0 ? "" : $" WHERE {string.Join(" AND ", where)}";

            var sql = $@"
SELECT o.id,
       o.obstacle_name        AS ObstacleName,
       o.height_m             AS HeightMeters,
       o.is_draft             AS IsDraft,
       o.created_utc          AS CreatedUtc,
       o.review_status        AS ReviewStatus,
       o.review_comment       AS ReviewComment,
       createdUser.UserName   AS CreatedByUserName,
       assignedUser.UserName  AS AssignedToUserName
FROM obstacles o
LEFT JOIN AspNetUsers createdUser  ON createdUser.Id = o.created_by_user_id
LEFT JOIN AspNetUsers assignedUser ON assignedUser.Id = o.assigned_to_user_id
{whereClause}
ORDER BY o.id DESC;";

            var rows = await con.QueryAsync<ObstacleListItem>(sql, parameters);

            return View(new ObstacleListVm
            {
                Filter = filter,
                Items = rows
            });
        }

        // Konverterer DateTime fra Local/Unspecified til UTC
        private static DateTime? NormalizeToUtc(DateTime? value)
        {
            if (value is null)
                return null;

            var dt = value.Value;
            return dt.Kind switch
            {
                DateTimeKind.Utc => dt,
                DateTimeKind.Unspecified => DateTime.SpecifyKind(dt, DateTimeKind.Local).ToUniversalTime(),
                _ => dt.ToUniversalTime()
            };
        }

        // =========================================================
        // 5) DETALJVISNING – Viser ett hinder
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            const string sql = @"
SELECT o.id,
       o.geojson              AS GeoJson,
       o.obstacle_name        AS ObstacleName,
       o.height_m             AS HeightMeters,
       o.obstacle_description AS Description,
       o.is_draft             AS IsDraft,
       o.created_utc          AS CreatedUtc,
       o.review_status        AS ReviewStatus,
       o.review_comment       AS ReviewComment,
       createdUser.UserName   AS CreatedByUserName,
       assignedUser.UserName  AS AssignedToUserName
FROM obstacles o
LEFT JOIN AspNetUsers createdUser  ON createdUser.Id = o.created_by_user_id
LEFT JOIN AspNetUsers assignedUser ON assignedUser.Id = o.assigned_to_user_id
WHERE o.id = @id;";

            using var con = CreateConnection();
            var row = await con.QuerySingleOrDefaultAsync<ObstacleDetailsVm>(sql, new { id });

            if (row == null)
                return NotFound();

            return View(row);
        }

        // =========================================================
        // 6) ENDRE HINDER (kun eier)
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            const string sql = @"
SELECT id,
       geojson,
       obstacle_name         AS ObstacleName,
       height_m              AS HeightM,
       obstacle_description  AS ObstacleDescription,
       is_draft              AS IsDraft,
       created_utc           AS CreatedUtc,
       created_by_user_id
FROM obstacles
WHERE id = @id
  AND created_by_user_id = @UserId;";

            using var con = CreateConnection();
            var row = await con.QuerySingleOrDefaultAsync<ObstacleData>(sql, new { id, UserId = userId });

            if (row == null)
                return Forbid(); // Ikke ditt hinder

            // Mapper databasen til ViewModel
            var vm = new ObstacleEditVm
            {
                Id = row.Id,
                ObstacleName = row.ObstacleName,
                HeightValue = row.HeightM,
                HeightUnit = "m",
                Description = row.ObstacleDescription,
                SaveAsDraft = row.IsDraft
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ObstacleEditVm vm)
        {
            // Samme validering som på opprett
            if (string.IsNullOrWhiteSpace(vm.ObstacleName))
                ModelState.AddModelError(nameof(vm.ObstacleName), "Skriv hva det er.");

            if (vm.HeightValue is null || vm.HeightValue < 0)
                ModelState.AddModelError(nameof(vm.HeightValue), "Oppgi høyde.");

            if (!ModelState.IsValid)
                return View(vm);

            double heightMeters = vm.HeightValue!.Value;
            if (string.Equals(vm.HeightUnit, "ft", StringComparison.OrdinalIgnoreCase))
                heightMeters = Math.Round(heightMeters * 0.3048, 0);

            const string sql = @"
UPDATE obstacles
SET obstacle_name        = @Name,
    height_m             = @HeightM,
    obstacle_description = @Descr,
    is_draft             = @IsDraft
WHERE id = @Id;";

            using var con = CreateConnection();
            await con.ExecuteAsync(sql, new
            {
                Id = vm.Id,
                Name = vm.ObstacleName,
                HeightM = (int?)Math.Round(heightMeters, 0),
                Descr = vm.Description,
                IsDraft = vm.SaveAsDraft ? 1 : 0
            });

            return RedirectToAction(nameof(Details), new { id = vm.Id });
        }

        // =========================================================
        // 7) SLETT HINDER (kun eier)
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            const string sql = @"
DELETE FROM obstacles
WHERE id = @id
  AND created_by_user_id = @UserId;";

            using var con = CreateConnection();
            var affected = await con.ExecuteAsync(sql, new { id, UserId = userId });

            if (affected == 0)
                return Forbid();

            return RedirectToAction(nameof(List));
        }

        // =========================================================
        // 8) GODKJENN / AVVIS (REVIEW)
        // =========================================================

        [HttpPost]
        [Authorize(Roles = "Admin,Approver")]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> Approve(int id, string? reviewComment)
            => SetReviewStatus(id, ObstacleStatus.Approved, reviewComment);

        [HttpPost]
        [Authorize(Roles = "Admin,Approver")]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> Reject(int id, string? reviewComment)
            => SetReviewStatus(id, ObstacleStatus.Rejected, reviewComment);

        // Felles logikk for Approve/Reject
        private async Task<IActionResult> SetReviewStatus(int id, ObstacleStatus status, string? reviewComment)
        {
            const string sql = @"
UPDATE obstacles
SET review_status = @Status,
    review_comment = @Comment,
    assigned_to_user_id = @AssignedTo
WHERE id = @Id;";

            using var con = CreateConnection();
            var rows = await con.ExecuteAsync(sql, new
            {
                Id = id,
                Status = status.ToString(),
                Comment = reviewComment,
                AssignedTo = User.FindFirstValue(ClaimTypes.NameIdentifier)
            });

            if (rows == 0)
                return NotFound();

            TempData["StatusMessage"] = status == ObstacleStatus.Approved
                ? "Hinderet er godkjent."
                : "Hinderet er avvist.";

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
