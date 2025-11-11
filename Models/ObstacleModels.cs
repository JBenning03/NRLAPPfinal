namespace NRLApp.Models
{
    public sealed class DrawState
    {
        // GeoJSON FeatureCollection (string)
        public string? GeoJson { get; set; }
    }

    public sealed class ObstacleMetaVm
    {
        // Vises i Meta.cshtml
        public string? ObstacleName { get; set; }

        public double? HeightValue { get; set; }   // det brukeren taster
        public string HeightUnit { get; set; } = "m"; // "m" eller "ft"

        public string? Description { get; set; }
        public bool SaveAsDraft { get; set; }
    }
}
