using parkmeter_management.Api.DTOs;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Api.Mappers
{
    public static class ConfigMapper
    {
        public static ParkMeterConfigDto ToDto(
            this ParkMeterConfig config)
            => new ParkMeterConfigDto
            {
                Id = config.Id,
                ParkmeterId = config.ParkmeterId,
                MaxDuration = config.MaxDuration,
                StrategyType = config.StrategyType,
                PricePerHour = config.PricePerHour,
                PeakRate = config.PeakRate,
                OffPeakRate = config.OffPeakRate,
                WeekdayRate = config.WeekdayRate,
                WeekendRate = config.WeekendRate,
                CreatedAt = config.CreatedAt,
                UpdatedAt = config.UpdatedAt
            };
    }
}