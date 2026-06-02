using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Domain.Entities
{
    public class ParkMeterConfig
    {

        private ParkMeterConfig() { }

        public ParkMeterConfig(
            Guid parkmeterId,
            int maxDuration,
            PricingStrategyType strategyType,
            decimal? pricePerHour,
            decimal? peakRate,
            decimal? offPeakRate,
            decimal? weekdayRate,
            decimal? weekendRate)
        {
            Id = Guid.NewGuid();
            ParkmeterId = parkmeterId;
            MaxDuration = maxDuration;
            StrategyType = strategyType;
            PricePerHour = pricePerHour;
            PeakRate = peakRate;
            OffPeakRate = offPeakRate;
            WeekdayRate = weekdayRate;
            WeekendRate = weekendRate;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public Guid Id { get; private set; } = Guid.NewGuid();
        public Guid ParkmeterId { get; private set; }
        public int MaxDuration { get; private set; }

        public PricingStrategyType StrategyType { get; private set; }
        = PricingStrategyType.FLAT_RATE;

        public decimal? PricePerHour { get; private set; }
        public decimal? PeakRate { get; private set; } 
        public decimal? OffPeakRate { get; private set; }

        public decimal? WeekdayRate { get; private set; }
        public decimal? WeekendRate { get; private set; }

        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;
        public void UpdateConfig(int maxDuration,PricingStrategyType strategyType,decimal? pricePerHour,
                                decimal? peakRate,decimal? offPeakRate,decimal? weekdayRate,decimal? weekendRate)
        {
            MaxDuration = maxDuration;
            StrategyType = strategyType;
            PricePerHour = pricePerHour ?? 0;
            PeakRate = peakRate ?? 0;
            OffPeakRate = offPeakRate ?? 0;
            WeekdayRate = weekdayRate ?? 0;
            WeekendRate = weekendRate ?? 0;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
