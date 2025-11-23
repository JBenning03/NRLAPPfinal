using NRLApp.Models.Obstacles;

public class ObstacleListItem
{
    public int Id { get; set; }
    public string? ObstacleName { get; set; }
    public int? HeightMeters { get; set; }
    public bool IsDraft { get; set; }
    public DateTime CreatedUtc { get; set; }
    public ObstacleStatus? ReviewStatus { get; set; }
    public string? ReviewComment { get; set; }

    public string? CreatedByUserName { get; set; }   // brukes fortsatt i detaljer
    public string? AssignedToUserName { get; set; }

    // NY:
    public string? OrganizationName { get; set; }
}
