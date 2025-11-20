using System;

namespace NRLApp.Models.Obstacles
{
    public sealed class ObstacleData
    {
        public int Id { get; set; }

        // Ny geometri + metadata
        public string? GeoJson { get; set; }
        public string? ObstacleName { get; set; }
        public int? HeightM { get; set; }                 // høyde i meter
        public string? ObstacleDescription { get; set; }
        public bool IsDraft { get; set; }
        public DateTime CreatedUtc { get; set; }
    }
}
