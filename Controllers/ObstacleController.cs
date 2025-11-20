using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using NRLApp.Models;

namespace NRLApp.Controllers
{
    [Authorize]
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
                : (System.Text.Json.JsonSerializer.Deserialize<DrawState>(DrawJson!) ?? new DrawState());

        private void SaveDrawState(DrawState s)
            => DrawJson = System.Text.Json.JsonSerializer.Serialize(s);

        // =========================================================
        // 1) TEGN OMRÅDE (AREA)
        // =========================================================

        [HttpGet]
        public IActionResult Area() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
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

        // =========================================================
        // 2) METADATA (META)
        // =========================================================

        [HttpGet]
        public IActionResult Meta()
        {
            var s = GetDrawState();

            if (string.IsNullOrWhiteSpace(s.GeoJson))
                return RedirectToAction(nameof(Area));

            TempData.Keep(nameof(DrawJson));
            return View(new ObstacleMetaVm());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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

            // Konverter høyde til meter
            double heightMeters = vm.HeightValue!.Value;
            if (string.Equals(vm.HeightUnit, "ft", StringComparison.OrdinalIgnoreCase))
            {
                heightMeters = Math.Round(heightMeters * 0.3048, 0);
            }

            bool isDraft = string.Equals(action, "draft", StringComparison.OrdinalIgnoreCase) || vm.SaveAsDraft;

            // Hent bruker-ID
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            const string sql = @"
INSERT INTO obstacles (
    geojson,
    obstacle_name,
    height_m,
    obstacle_description,
    is_draft,
    created_utc,
    created_by_user_id
)
VALUES (
    @GeoJson,
    @Name,
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
                Name = vm.ObstacleName,
                HeightM = (int?)Math.Round(heightMeters, 0),
                Descr = vm.Description,
                IsDraft = isDraft ? 1 : 0,
                CreatedByUserId = userId
            });

            DrawJson = null;
            return RedirectToAction(nameof(Thanks), new { draft = isDraft });
        }

        // =========================================================
        // 3) TAKK-SIDE
        // =========================================================

        [HttpGet]
        public IActionResult Thanks(bool draft = false)
        {
            ViewBag.Draft = draft;
            return View();
        }

        // =========================================================
        // 4) LISTE OVER HINDRE + FILTRERING
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] ObstacleListFilter filter)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            using var con = CreateConnection();
            var where = new List<string>();
            var parameters = new DynamicParameters();

            if (filter.Id.HasValue)
            {
                where.Add("id = @Id");
                parameters.Add("Id", filter.Id.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.ObstacleName))
            {
                where.Add("LOWER(obstacle_name) LIKE @ObstacleName");
                parameters.Add("ObstacleName", $"%{filter.ObstacleName.Trim().ToLowerInvariant()}%");
            }

            if (filter.MinHeightMeters.HasValue)
            {
                where.Add("height_m >= @MinHeight");
                parameters.Add("MinHeight", filter.MinHeightMeters.Value);
            }

            if (filter.MaxHeightMeters.HasValue)
            {
                where.Add("height_m <= @MaxHeight");
                parameters.Add("MaxHeight", filter.MaxHeightMeters.Value);
            }

            if (filter.Status.HasValue)
            {
                where.Add("is_draft = @IsDraft");
                parameters.Add("IsDraft", filter.Status.Value == ObstacleListStatusFilter.Draft ? 1 : 0);
            }

            var createdFromUtc = NormalizeToUtc(filter.CreatedFrom);
            if (createdFromUtc.HasValue)
            {
                where.Add("created_utc >= @CreatedFrom");
                parameters.Add("CreatedFrom", createdFromUtc.Value);
            }

            var createdToUtc = NormalizeToUtc(filter.CreatedTo);
            if (createdToUtc.HasValue)
            {
                where.Add("created_utc <= @CreatedTo");
                parameters.Add("CreatedTo", createdToUtc.Value);
            }

            var whereClause = where.Count == 0 ? string.Empty : $" WHERE {string.Join(" AND ", where)}";

            var sql = $@"
SELECT id,
       obstacle_name    AS ObstacleName,
       height_m         AS HeightMeters,
       is_draft         AS IsDraft,
       created_utc      AS CreatedUtc
FROM obstacles{whereClause}
ORDER BY id DESC;";

            var rows = await con.QueryAsync<ObstacleListItem>(sql, parameters);
            return View(new ObstacleListVm
            {
                Filter = filter,
                Items = rows
            });
        }

        private static DateTime? NormalizeToUtc(DateTime? value)
        {
            if (value is null)
                return null;

            var dt = value.Value;
            if (dt.Kind == DateTimeKind.Utc)
                return dt;

            if (dt.Kind == DateTimeKind.Unspecified)
                return DateTime.SpecifyKind(dt, DateTimeKind.Local).ToUniversalTime();

            return dt.ToUniversalTime();
        }

        // =========================================================
        // 5) DETALJVISNING
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Details(int id)
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
WHERE id = @id;";

            using var con = CreateConnection();
            var row = await con.QuerySingleOrDefaultAsync<ObstacleData>(sql, new { id });

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
            if (string.IsNullOrWhiteSpace(vm.ObstacleName))
                ModelState.AddModelError(nameof(vm.ObstacleName), "Skriv hva det er.");
            if (vm.HeightValue is null || vm.HeightValue < 0)
                ModelState.AddModelError(nameof(vm.HeightValue), "Oppgi høyde.");

            if (!ModelState.IsValid)
                return View(vm);

            double heightMeters = vm.HeightValue!.Value;
            if (string.Equals(vm.HeightUnit, "ft", StringComparison.OrdinalIgnoreCase))
            {
                heightMeters = Math.Round(heightMeters * 0.3048, 0);
            }

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
    }
}
