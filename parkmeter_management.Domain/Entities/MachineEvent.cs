using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Domain.Entities
{
    public class MachineEvent
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ParkmeterId { get; set; }
        public MachineEventType EventType { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Parkmeter? Parkmeter { get; set; }
    }
}