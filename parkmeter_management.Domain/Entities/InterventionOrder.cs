using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Domain.Entities
{
    public class InterventionOrder
    {
        private InterventionOrder() { }

        public InterventionOrder(Guid parkmeterId, Guid operatorId, InterventionType type)
        {
            Id = Guid.NewGuid();
            ParkmeterId = parkmeterId;
            OperatorId = operatorId;
            Type = type;
            Status = InterventionOrderStatus.PENDING;
            CreatedAt = DateTime.UtcNow;
        }

        public Guid Id { get; private set; } = Guid.NewGuid();
        public Guid ParkmeterId { get; private set; }
        public Parkmeter? Parkmeter { get; set; }  

        public Guid OperatorId { get; private set; }
        public InterventionType Type { get; private set; }
        public InterventionOrderStatus Status { get; private set; }
            = InterventionOrderStatus.PENDING;
        public DateTime CreatedAt { get; private set; }
            = DateTime.UtcNow;
        public DateTime? CompletedAt { get; private set; }
        public User? Operator { get; set; }

        public void Complete()
        {
            Status = InterventionOrderStatus.COMPLETED;
            CompletedAt = DateTime.UtcNow;
        }
    }

}