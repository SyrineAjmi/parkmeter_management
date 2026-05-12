using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Domain.Entities
{
    public class ParkMeterConfig
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ParkmeterId { get; set; }
        public int MaxDuration { get; set; }

        public PricingStrategyType StrategyType { get; set; }
        = PricingStrategyType.FLAT_RATE;

        public decimal? PricePerHour { get; set; }
        public decimal? PeakRate { get; set; } 
        public decimal? OffPeakRate { get; set; }

        public decimal? WeekdayRate { get; set; }
        public decimal? WeekendRate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
