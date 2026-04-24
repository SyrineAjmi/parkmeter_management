using parkmeter_management.Api.DTOs;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Api.Mappers
{
    public static class ParkMeterEventMapper
    {
        public static ParkMeterEventDto ToDto(this ParkMeterEvent e)
            => new ParkMeterEventDto
            {
                Id = e.Id,
                ParkmeterId = e.ParkmeterId,
                EventType = e.EventType.ToString(),
                CreatedAt = e.CreatedAt
            };

        public static IEnumerable<ParkMeterEventDto> ToDtos(
            this IEnumerable<ParkMeterEvent> events)
            => events.Select(e => e.ToDto());
    }
}