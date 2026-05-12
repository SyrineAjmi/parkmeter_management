using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Api.Requests.Configuration
{
    public class CreateConfigRequest
    {
        public Guid ParkmeterId { get; set; }
        public int MaxDuration { get; set; }

        // strategy selector
        public PricingStrategyType StrategyType { get; set; }
            = PricingStrategyType.FLAT_RATE;

        // flat rate
        public decimal? PricePerHour { get; set; }

        // time of day
        public decimal? PeakRate { get; set; }
        public decimal? OffPeakRate { get; set; }

        // weekday/weekend
        public decimal? WeekdayRate { get; set; }
        public decimal? WeekendRate { get; set; }
    }
}