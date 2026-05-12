using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Api.DTOs
{
    public class ParkMeterConfigDto
    {
        public Guid Id { get; set; }
        public Guid ParkmeterId { get; set; }
        public int MaxDuration { get; set; }

        // strategy
        public PricingStrategyType StrategyType { get; set; }

        // flat rate
        public decimal? PricePerHour { get; set; }

        // time of day
        public decimal? PeakRate { get; set; }
        public decimal? OffPeakRate { get; set; }

        // weekday/weekend
        public decimal? WeekdayRate { get; set; }
        public decimal? WeekendRate { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}