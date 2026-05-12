using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Application.Queries.Parkmeters
{
    public class GetParkmetersBySiteQueryHandler
        : IRequestHandler<GetParkmetersBySiteQuery, List<Parkmeter>>
    {
        private readonly IParkmeterRepository _parkmeterRepository;

        public GetParkmetersBySiteQueryHandler(
            IParkmeterRepository parkmeterRepository)
        {
            _parkmeterRepository = parkmeterRepository;
        }

        public async Task<List<Parkmeter>> Handle(
            GetParkmetersBySiteQuery query, CancellationToken ct)
        {
            var result = await _parkmeterRepository
                .GetBySiteAsync(query.SiteId);
            return result.ToList();
        }
    }
}