namespace NRLApp.Models.Obstacles
{
    public enum ObstacleListStatusFilter
    {
        Draft,
        Pending
    }

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

    public sealed class ObstacleListVm
    {
        public ObstacleListFilter Filter { get; set; } = new();
        public IEnumerable<ObstacleListItem> Items { get; set; } = Array.Empty<ObstacleListItem>();
    }
}