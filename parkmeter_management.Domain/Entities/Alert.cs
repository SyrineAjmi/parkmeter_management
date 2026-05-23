using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Domain.Entities
{
    public class Alert
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ParkmeterId { get; set; }
        public Parkmeter? Parkmeter { get; set; }

        public AlertType Type { get; set; }
        public string Description { get; set; } = string.Empty;
        public AlertStatus Status { get; set; } = AlertStatus.NOT_RESOLVED;
        public Priority Priority { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }
    }

}
