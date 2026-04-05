namespace parkmeter_management.Api.DTOs
{
    public class AlertDto
    {
        public Guid Id { get; set; }
        public Guid ParkmeterId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; } 

    }
}