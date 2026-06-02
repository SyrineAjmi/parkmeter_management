using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Domain.Entities
{
    public class Parkmeter
    {
        private Parkmeter() { }

        public Parkmeter(string serialNumber, Guid siteId)
        {
            Id = Guid.NewGuid();
            SerialNumber = serialNumber;
            SiteId = siteId;
            Status = MeterStatus.PENDING_ACTIVATION;
            CashAmount = 0;
            CreatedAt = DateTime.UtcNow;
        }
        public Guid Id { get; private set; } = Guid.NewGuid();
        public string SerialNumber { get; private set; } = string.Empty;
        public Guid SiteId { get; private set; }
        public MeterStatus Status { get; private set; } = MeterStatus.PENDING_ACTIVATION;
        public decimal CashAmount { get; private set; } = 0; 
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public Site? Site { get; set; }
        public ParkMeterConfig? Configuration { get; set; }

        public void UpdateStatus(MeterStatus status) => Status = status;
        public void UpdateCashAmount(decimal amount) => CashAmount = amount;
        public void UpdateSite(Guid siteId) => SiteId = siteId;
    }

}
