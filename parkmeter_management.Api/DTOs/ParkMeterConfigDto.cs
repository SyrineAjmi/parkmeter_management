namespace parkmeter_management.Api.DTOs
{
    public class ParkMeterConfigDto
    {
        public Guid Id { get; set; }
        public Guid ParkmeterId { get; set; }
        public decimal PricePerHour { get; set; }
        public int MaxDuration { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
    }

}