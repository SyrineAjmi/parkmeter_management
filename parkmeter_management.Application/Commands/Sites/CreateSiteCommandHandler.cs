using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Domain.Entities;
using System.Net;
using System.Xml.Linq;

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
            (                
                command.Name,
                command.Address,
                command.Description,
                command.Latitude,
                command.Longitude
            );

            await _siteRepository.AddAsync(site);
            return site.Id;
        }
    }
}