namespace parkmeter_management.Application.Interfaces.Services
{
    public interface IAiReportService
    {
        Task<AiReportResult> GenerateFinancialReportAsync(int month, int year);
    }

    public class AiReportResult
    {
        public string Period { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public decimal TotalRevenue { get; set; }
        public int TotalTransactions { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }
}