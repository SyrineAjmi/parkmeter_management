using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Domain.Entities
{
    public class Parkmeter
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string SerialNumber { get; set; } = string.Empty;
        public Guid SiteId { get; set; }
        public MeterStatus Status { get; set; } = MeterStatus.IN_SERVICE;
        public decimal CashAmount { get; set; } = 0; 
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Site? Site { get; set; }
        public ParkMeterConfig? Configuration { get; set; }
    }

}
