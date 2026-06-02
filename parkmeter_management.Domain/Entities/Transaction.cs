using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Domain.Entities
{
    public class Transaction
    {
        private Transaction() { }

        public Transaction(Guid parkmeterId, Guid sessionId, decimal amount, int duration, TransactionStatus status)
        {
            Id = Guid.NewGuid();
            ParkmeterId = parkmeterId;
            SessionId = sessionId;
            Amount = amount;
            Duration = duration;
            Status = status;
            CreatedAt = DateTime.UtcNow;
        }

        public Guid Id { get; private set; } = Guid.NewGuid();
        public Guid ParkmeterId { get; private set; }
        public Parkmeter? Parkmeter { get; set; }

        public Guid SessionId { get; private set; }
        public decimal Amount { get; private set; }
        public int Duration { get; private set; }
        public TransactionStatus Status { get; private set; }
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    }
    
}
