namespace parkmeter_management.Api.DTOs.Stats
{
    public class CurrentSessionDto
    {
        public Guid ParkmeterID { get; set; }
        public string SerialNumber { get; set; } = string.Empty;
        public DateTime SessionStart { get; set; }
        public int TotalTransactions { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
    }   
}
