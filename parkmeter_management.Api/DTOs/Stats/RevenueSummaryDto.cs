namespace parkmeter_management.Api.DTOs.Stats
{
    public class RevenueSummaryDto
    {
        public decimal TotalRevenue { get; set; }
        public int TotalTransactions { get; set; }
        public int CompletedTransactions { get; set; }
        public int FailedTransactions { get; set; }
        public int CancelledTransactions { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
    }
}
