using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Domain.Entities
{
    public class InterventionOrder
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ParkmeterId { get; set; }
        public Guid OperatorId { get; set; }
        public InterventionType Type { get; set; }
        public InterventionOrderStatus Status { get; set; }
            = InterventionOrderStatus.PENDING;
        public DateTime CreatedAt { get; set; }
            = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public User? Operator { get; set; }
    }

}