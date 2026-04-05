using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Application.Queries.Sites
{
    public class GetAllSitesQueryHandler
        : IRequestHandler<GetAllSitesQuery, List<Site>>
    {
        private readonly ISiteRepository _siteRepository;

        public GetAllSitesQueryHandler(ISiteRepository siteRepository)
        {
            _siteRepository = siteRepository;
        }

        public async Task<List<Site>> Handle(
            GetAllSitesQuery request, CancellationToken ct)
        {
            var sites = await _siteRepository.GetAllAsync();
            return sites.ToList();
        }
    }
}