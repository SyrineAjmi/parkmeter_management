namespace parkmeter_management.Api.DTOs
{
    public class MachineEventDto
    {
        public Guid Id { get; set; }
        public Guid ParkmeterId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
