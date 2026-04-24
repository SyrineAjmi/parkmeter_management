using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Domain.Entities
{
    public class ParkMeterEvent
    {
        public Guid Id { get; set; }
        public Guid ParkmeterId { get; set; }
        public ParkMeterEventType EventType { get; set; }
        public DateTime CreatedAt { get; set; }

        public Parkmeter Parkmeter { get; set; } = null!;
    }
}