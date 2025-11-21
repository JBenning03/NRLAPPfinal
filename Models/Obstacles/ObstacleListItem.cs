namespace NRLApp.Models.Obstacles
{
    public sealed class ObstacleListItem
    {
        public int Id { get; set; }

        // Ny feltnavn (må stemme med List.cshtml)
        public string? ObstacleName { get; set; }
        public int? HeightMeters { get; set; }
        public bool IsDraft { get; set; }
        public DateTime CreatedUtc { get; set; }

        // TIL REVIEW
        public ObstacleStatus? ReviewStatus { get; set; }
        public string? ReviewComment { get; set; }
        public string? CreatedByUserName { get; set; }
        public string? AssignedToUserName { get; set; }
    }
}
