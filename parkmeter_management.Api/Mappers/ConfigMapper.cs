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
                ParkmeterId = config.ParkmeterId,
                PricePerHour = config.PricePerHour,
                MaxDuration = config.MaxDuration,
                CreatedAt = config.CreatedAt,
                UpdatedAt = config.UpdatedAt
            };
    }
}