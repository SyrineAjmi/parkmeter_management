using MediatR;
using parkmeter_management.Application.Commands.Sites;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Application.Managers.Interfaces;
using parkmeter_management.Application.Queries.Sites;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Application.Managers.Implementations
{
    public class SiteManager : ISiteManager
    {
        private readonly IMediator _mediator;
        private readonly ISiteRepository _siteRepository;

        public SiteManager(IMediator mediator, ISiteRepository siteRepository)
        {
            _mediator = mediator;
            _siteRepository = siteRepository;
        }

        public async Task<Guid> CreateSite(string name, string address,
            string description)
            => await _mediator.Send(new CreateSiteCommand
            {
                Name = name,
                Address = address,
                Description = description,
            });

        public async Task<bool> UpdateSite(Guid id, string name,
            string address, string description)
        {
            var site = await _siteRepository.GetByIdAsync(id);
            if (site == null) return false;

            site.Name = name;
            site.Address = address;
            site.Description = description;
            await _siteRepository.UpdateAsync(site);
            return true;
        }

        public async Task<bool> DeleteSite(Guid id)
        {
            var site = await _siteRepository.GetByIdAsync(id);
            if (site == null) return false;

            await _siteRepository.DeleteAsync(id);
            return true;
        }

        public async Task<List<Site>> GetAllSites()
            => await _mediator.Send(new GetAllSitesQuery());

        public async Task<Site?> GetById(Guid id)
            => await _siteRepository.GetByIdAsync(id);
    }
}