using System.Text.Json;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using NRLApp.Models;

namespace NRLApp.Controllers
{
    public class ObstacleController : Controller
    {
        private readonly IConfiguration _config;
        public ObstacleController(IConfiguration config) => _config = config;

        private MySqlConnection CreateConnection()
            => new MySqlConnection(_config.GetConnectionString("DefaultConnection"));

        // Holder geometri mellom steg (lagres i cookie)
        [TempData] public string? DrawJson { get; set; }

        private DrawState GetDrawState()
            => string.IsNullOrWhiteSpace(DrawJson)
                ? new DrawState()
                : (JsonSerializer.Deserialize<DrawState>(DrawJson!) ?? new DrawState());

        private void SaveDrawState(DrawState s)
            => DrawJson = JsonSerializer.Serialize(s);

        // ===== STEP 1: Tegn marker/linje/område =====
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

            return View(new ObstacleMetaVm());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Meta(ObstacleMetaVm vm, string? action)
        {
            var s = GetDrawState();
            if (string.IsNullOrWhiteSpace(s.GeoJson))
                return RedirectToAction(nameof(Area));

            // Enkelt input-sjekk
            if (string.IsNullOrWhiteSpace(vm.ObstacleName))
                ModelState.AddModelError(nameof(vm.ObstacleName), "Skriv hva det er.");
            if (vm.HeightValue is null || vm.HeightValue < 0)
                ModelState.AddModelError(nameof(vm.HeightValue), "Oppgi høyde.");

            if (!ModelState.IsValid) return View(vm);

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
            await con.ExecuteAsync(sql, new
            {
                GeoJson = s.GeoJson,
                Name = vm.ObstacleName,
                HeightM = (int?)Math.Round(heightMeters, 0),
                Descr = vm.Description,
                IsDraft = isDraft ? 1 : 0
            });

            // Tøm state og send til takk
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

        // ===== (valgfritt) eksisterende liste =====
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
            var rows = await con.QueryAsync<dynamic>(sql);
            return View(rows);
        }
    }
}

