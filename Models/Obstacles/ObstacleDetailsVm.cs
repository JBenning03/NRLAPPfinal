namespace NRLApp.Models.Obstacles
{
/// <summary>
/// Viser alle felt for et hinder, inkludert review-info.
/// <summary>
    public class ObstacleDetailsVm
    {
        public int Id { get; set; }
        public string? ObstacleName { get; set; }
        public int? HeightMeters { get; set; }
        public string? Description { get; set; }
        public string GeoJson { get; set; } = "{}";
        public bool IsDraft { get; set; }
        public DateTime CreatedUtc { get; set; }

        // TIL REVIEW
        public ObstacleStatus? ReviewStatus { get; set; }
        public string? ReviewComment { get; set; }
        public string? CreatedByUserName { get; set; }
        public string? AssignedToUserName { get; set; }
    }
}
