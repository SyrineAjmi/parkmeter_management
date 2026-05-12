namespace parkmeter_management.Api.DTOs.Stats
{
    public class RevenueByParkmeterDto
    {
        public Guid ParkmeterID { get; set; }
        public string SerialNumber { get; set; } = string.Empty;
        public decimal TotalRevenue { get; set; }
        public int TotalTransactions { get; set; }
    }
}
