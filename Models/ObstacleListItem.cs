namespace NRLApp.Models
{
    public sealed class ObstacleListItem
    {
        public int Id { get; set; }

        // Ny feltnavn (må stemme med List.cshtml)
        public string? ObstacleName { get; set; }
        public int? HeightMeters { get; set; }
        public bool IsDraft { get; set; }
        public DateTime CreatedUtc { get; set; }
    }
}
