using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;

namespace parkmeter_management.Application.Commands.Sites
{
    public class UpdateSiteCommandHandler
        : IRequestHandler<UpdateSiteCommand, bool>
    {
        private readonly ISiteRepository _siteRepository;

        public UpdateSiteCommandHandler(ISiteRepository siteRepository)
        {
            _siteRepository = siteRepository;
        }

        public async Task<bool> Handle(
            UpdateSiteCommand command, CancellationToken ct)
        {
            var site = await _siteRepository.GetByIdAsync(command.Id);
            if (site == null) return false;

            site.Name = command.Name;
            site.Address = command.Address;
            site.Description = command.Description;
            site.Latitude = command.Latitude;
            site.Longitude = command.Longitude;

            await _siteRepository.UpdateAsync(site);
            return true;
        }
    }
}