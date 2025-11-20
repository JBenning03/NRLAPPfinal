using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System;
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

            // Lagrer GeoJSON i TempData mellom steg
            SaveDrawState(new DrawState { GeoJson = geoJson });

            // Gå videre til metadata
            return RedirectToAction(nameof(Meta));
        }

        // =========================================================
        // 2) METADATA (META)
        // =========================================================

        [HttpGet]
        public IActionResult Meta()
        {
            var s = GetDrawState();

            // Hvis noen går direkte til /Obstacle/Meta uten å ha vært innom Area
            if (string.IsNullOrWhiteSpace(s.GeoJson))
                return RedirectToAction(nameof(Area));

            // Behold DrawJson videre til POST
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

            const string sql = @"
INSERT INTO obstacles (geojson, obstacle_name, height_m, obstacle_description, is_draft, created_utc)
VALUES (@GeoJson, @Name, @HeightM, @Descr, @IsDraft, UTC_TIMESTAMP());";

            using var con = CreateConnection();
            try
            {
                await con.ExecuteAsync(sql, new
                {
                    GeoJson = geoJsonToSave,
                    Name = vm.ObstacleName,
                    HeightM = (int?)Math.Round(heightMeters, 0),
                    Descr = vm.Description,
                    IsDraft = isDraft ? 1 : 0
                });
            }
            catch
            {
                // Evt. logging
            }

            // Tøm state og send til takk
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
        // 4) LISTE OVER HINDRE
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> List()
        {
            using var con = CreateConnection();
            const string sql = @"
SELECT id,
       obstacle_name    AS ObstacleName,
       height_m         AS HeightMeters,
       is_draft         AS IsDraft,
       created_utc      AS CreatedUtc
FROM obstacles
ORDER BY id DESC;";
            var rows = await con.QueryAsync<ObstacleListItem>(sql);
            return View(rows);
        }

        // =========================================================
        // 5) DETALJVISNING
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            const string sql = @"
SELECT id,
       geojson,
       obstacle_name         AS ObstacleName,
       height_m              AS HeightM,
       obstacle_description  AS ObstacleDescription,
       is_draft              AS IsDraft,
       created_utc           AS CreatedUtc
FROM obstacles
WHERE id = @id;";

            using var con = CreateConnection();
            var row = await con.QuerySingleOrDefaultAsync<ObstacleData>(sql, new { id });

            if (row == null)
                return NotFound();

            return View(row);
        }

        // =========================================================
        // 6) ENDRE HINDER
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            const string sql = @"
SELECT id,
       geojson,
       obstacle_name         AS ObstacleName,
       height_m              AS HeightM,
       obstacle_description  AS ObstacleDescription,
       is_draft              AS IsDraft,
       created_utc           AS CreatedUtc
FROM obstacles
WHERE id = @id;";

            using var con = CreateConnection();
            var row = await con.QuerySingleOrDefaultAsync<ObstacleData>(sql, new { id });

            if (row == null)
                return NotFound();

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
        // 7) SLETT HINDER
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            const string sql = "DELETE FROM obstacles WHERE id = @id;";
            using var con = CreateConnection();
            await con.ExecuteAsync(sql, new { id });

            return RedirectToAction(nameof(List));
        }

        // =========================================================
        // 8) KART / MAP (om du allerede har en Map()-action,
        //    kan du lime den inn her i stedet)
        // =========================================================

        // [HttpGet]
        // public async Task<IActionResult> Map()
        // {
        //     // Hvis du har en fungerende Map()-action fra før,
        //     // bruk den i stedet for denne stuben.
        //     using var con = CreateConnection();
        //     const string sql = @"
        //     SELECT id,
        //            obstacle_name    AS ObstacleName,
        //            height_m         AS HeightMeters,
        //            is_draft         AS IsDraft,
        //            created_utc      AS CreatedUtc
        //     FROM obstacles
        //     ORDER BY id DESC;";
        //
        //     var rows = await con.QueryAsync<ObstacleListItem>(sql);
        //     return View(rows);
        // }
    }
}
