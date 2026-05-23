using parkmeter_management.Api.DTOs;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Api.Mappers
{
    public static class AlertMapper
    {
        public static AlertDto ToDto(this Alert alert)
            => new AlertDto
            {
                Id = alert.Id,
                ParkmeterId = alert.ParkmeterId,
                ParkmeterSerial = alert.Parkmeter?.SerialNumber ?? string.Empty,
                Type = alert.Type.ToString(),
                Priority = alert.Priority.ToString(),
                Status = alert.Status.ToString(),
                Description = alert.Description,
                CreatedAt = alert.CreatedAt,
                ResolvedAt = alert.ResolvedAt
            };

        public static IEnumerable<AlertDto> ToDtos(
            this IEnumerable<Alert> alerts)
            => alerts.Select(a => a.ToDto());
    }
}