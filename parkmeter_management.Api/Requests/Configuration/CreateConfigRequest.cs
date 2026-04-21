namespace parkmeter_management.Api.Requests.Configuration
{
    public class CreateConfigRequest
    {
        public Guid ParkmeterId { get; set; }
        public decimal PricePerHour { get; set; }
        public int MaxDuration { get; set; }
    }
}