using parkmeter_management.Api.DTOs;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Api.Mappers
{
    public static class MachineEventMapper
    {
        public static MachineEventDto ToDto(this MachineEvent e) => new()
        {
            Id = e.Id,
            ParkmeterId = e.ParkmeterId,
            EventType = e.EventType.ToString(),
            Description = e.Description,
            CreatedAt = e.CreatedAt
        };

        public static List<MachineEventDto> ToDtos(this IEnumerable<MachineEvent> events)
            => events.Select(e => e.ToDto()).ToList();
    }
}
