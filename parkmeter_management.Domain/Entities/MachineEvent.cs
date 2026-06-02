using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Domain.Entities
{
    public class MachineEvent
    {
        private MachineEvent() { }

        public MachineEvent(Guid parkmeterId, MachineEventType eventType, string description)
        {
            Id = Guid.NewGuid();
            ParkmeterId = parkmeterId;
            EventType = eventType;
            Description = description;
            CreatedAt = DateTime.UtcNow;
        }
        public Guid Id { get; private set; } = Guid.NewGuid();
        public Guid ParkmeterId { get; private set; }
        public MachineEventType EventType { get; private set; }
        public string Description { get; private set; } = string.Empty;
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public Parkmeter? Parkmeter { get; set; }
    }
}