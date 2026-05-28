using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Application.Commands.Sites
{
    public class CreateSiteCommandHandler
        : IRequestHandler<CreateSiteCommand, Guid>
    {
        private readonly ISiteRepository _siteRepository;

        public CreateSiteCommandHandler(ISiteRepository siteRepository)
        {
            _siteRepository = siteRepository;
        }

        public async Task<Guid> Handle(
            CreateSiteCommand command, CancellationToken ct)
        {
            var site = new Site
            {
                Id = Guid.NewGuid(),
                Name = command.Name,
                Address = command.Address,
                Description = command.Description,
                Latitude = command.Latitude,
                Longitude = command.Longitude,
                CreatedAt = DateTime.UtcNow,
            };

            await _siteRepository.AddAsync(site);
            return site.Id;
        }
    }
}