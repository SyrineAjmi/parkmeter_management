namespace parkmeter_management.Api.
    s
{
    public class SessionDto
    {
        public Guid Id { get; set; }
        public Guid ParkmeterId { get; set; }
        public string ParkmeterSerial { get; set; } = string.Empty;
        public DateTime StartDateTime { get; set; }
        public DateTime? EndDateTime { get; set; }
        public int TotalTransactions { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}