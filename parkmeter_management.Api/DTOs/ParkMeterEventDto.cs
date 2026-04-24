namespace parkmeter_management.Api.DTOs
{
    public class ParkMeterEventDto
    {
        public Guid Id { get; set; }
        public Guid ParkmeterId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}