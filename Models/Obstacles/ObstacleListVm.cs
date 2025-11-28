namespace NRLApp.Models.Obstacles
{
    /// <summary>
    /// Filteringsverdier for listesiden.
    /// <summary>
    public enum ObstacleListStatusFilter
    {
        Draft,
        Pending
    }

    /// <summary>
    /// Parameterovjer som bygges fra forespørselens query-parametere.
    /// <summary>
    public sealed class ObstacleListFilter
    {
        public int? Id { get; set; }
        public string? ObstacleName { get; set; }
        public int? MinHeightMeters { get; set; }
        public int? MaxHeightMeters { get; set; }
        public ObstacleListStatusFilter? Status { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
    }

    /// <summary>
    /// Viewmodel for tabellen som vises i Obstacle/List
    /// <summary>
    public sealed class ObstacleListVm
    {
        public ObstacleListFilter Filter { get; set; } = new();
        public IEnumerable<ObstacleListItem> Items { get; set; } = Array.Empty<ObstacleListItem>();
    }
}
