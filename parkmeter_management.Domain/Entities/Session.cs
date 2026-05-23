using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Domain.Entities
{
    public class Session
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ParkmeterId { get; set; }
        public Parkmeter? Parkmeter { get; set; }
        public DateTime StartDateTime { get; set; } = DateTime.UtcNow;
        public DateTime? EndDateTime { get; set; }
        public int TotalTransactions { get; set; } = 0;
        public decimal TotalAmount { get; set; } = 0;
        public SessionStatus Status { get; set; }
            = SessionStatus.ACTIVE;
    }
}