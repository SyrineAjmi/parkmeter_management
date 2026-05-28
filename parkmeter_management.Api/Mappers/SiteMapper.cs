using parkmeter_management.Api.DTOs;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Api.Mappers
{
    public static class SiteMapper
    {
        public static SiteDto ToDto(this Site site)
            => new SiteDto
            {
                Id = site.Id,
                Name = site.Name,
                Address = site.Address,
                Description = site.Description,
                Latitude = site.Latitude,   
                Longitude = site.Longitude, 
                CreatedAt = site.CreatedAt
            };

        public static IEnumerable<SiteDto> ToDtos(
            this IEnumerable<Site> sites)
            => sites.Select(s => s.ToDto());
    }
}