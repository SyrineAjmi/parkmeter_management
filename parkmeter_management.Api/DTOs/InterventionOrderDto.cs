namespace parkmeter_management.Api.DTOs
{
    public class InterventionOrderDto
    {
        public Guid Id { get; set; }
        public Guid ParkmeterId { get; set; }
        public string ParkmeterSerial { get; set; } = string.Empty;  
        public Guid OperatorId { get; set; }
        public string OperatorName { get; set; }
            = string.Empty;
        public string Type { get; set; }
            = string.Empty;
        public string Status { get; set; }
            = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}