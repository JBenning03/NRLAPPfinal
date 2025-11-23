using NRLApp.Models.Obstacles;

public class ObstacleListItem
{
    /// <summary>
    /// Minimal representasjon av et hinder slik det vises i tabellen.
    /// Inneholder også review- og organisasjonsinformasjon til badges
    /// <summary>
    public int Id { get; set; }
    public string? ObstacleName { get; set; }
    public int? HeightMeters { get; set; }
    public bool IsDraft { get; set; }
    public DateTime CreatedUtc { get; set; }
    public ObstacleStatus? ReviewStatus { get; set; }
    public string? ReviewComment { get; set; }

    public string? CreatedByUserName { get; set; }   // brukes fortsatt i detaljer
    public string? AssignedToUserName { get; set; }

    public string? OrganizationName { get; set; }
}
