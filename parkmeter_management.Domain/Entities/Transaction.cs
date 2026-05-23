using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Domain.Entities
{
    public class Transaction
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ParkmeterId { get; set; }
        public Parkmeter? Parkmeter { get; set; }

        public Guid SessionId { get; set; }
        public decimal Amount { get; set; }
        public int Duration { get; set; }
        public TransactionStatus Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
    
}
