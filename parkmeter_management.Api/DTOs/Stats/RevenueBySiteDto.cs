namespace parkmeter_management.Api.DTOs.Stats
{
    public class RevenueBySiteDto
    {
        public Guid SiteId { get; set; }
        public string SiteName { get; set; } = string.Empty;
        public decimal TotalRevenue { get; set; }
        public int TotalTransactions { get; set; }
    }
}
