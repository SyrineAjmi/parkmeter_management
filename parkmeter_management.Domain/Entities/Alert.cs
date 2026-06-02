using parkmeter_management.Domain.Entities;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Domain.Entities
{
    public class Alert
    {
        private Alert() { }

        public Alert(Guid parkmeterId, AlertType type, string description, Priority priority)
        {
            Id = Guid.NewGuid();
            ParkmeterId = parkmeterId;
            Type = type;
            Description = description;
            Priority = priority;
            Status = AlertStatus.NOT_RESOLVED;
            CreatedAt = DateTime.UtcNow;
        }
        public Guid Id { get; private set; } = Guid.NewGuid();
        public Guid ParkmeterId { get; private set; }
        public Parkmeter? Parkmeter { get; set; }

        public AlertType Type { get; private set; }
        public string Description { get; private set; } = string.Empty;
        public AlertStatus Status { get; private set; } = AlertStatus.NOT_RESOLVED;
        public Priority Priority { get; private set; }
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; private set; }

        public void Resolve()
        {
            Status = AlertStatus.RESOLVED;
            ResolvedAt = DateTime.UtcNow;
        }

        public void UpdatePriority(Priority priority) => Priority = priority;
    }

}

