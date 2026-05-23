using parkmeter_management.Api.DTOs;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Api.Mappers
{
    public static class ParkmeterMapper
    {
        public static ParkmeterDto ToDto(this Parkmeter parkmeter)
            => new ParkmeterDto
            {
                Id = parkmeter.Id,
                SerialNumber = parkmeter.SerialNumber,
                SiteId = parkmeter.SiteId,
                SiteName = parkmeter.Site?.Name,
                Status = parkmeter.Status.ToString(),
                CashAmount = parkmeter.CashAmount,
                CreatedAt = parkmeter.CreatedAt
            };

        public static IEnumerable<ParkmeterDto> ToDtos(
            this IEnumerable<Parkmeter> parkmeters)
            => parkmeters.Select(p => p.ToDto());
    }
}