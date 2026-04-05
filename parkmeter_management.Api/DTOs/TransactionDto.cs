namespace parkmeter_management.Api.DTOs
{
    public class TransactionDto
    {
        public Guid Id { get; set; }
        public Guid ParkmeterId { get; set; }
        public Guid SessionId { get; set; }
        public decimal Amount { get; set; }
        public int Duration { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

    }

}

