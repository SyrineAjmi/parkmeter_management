using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Application.Queries.Parkmeters
{
    public class GetParkmetersByStatusQueryHandler
        : IRequestHandler<GetParkmetersByStatusQuery, List<Parkmeter>>
    {
        private readonly IParkmeterRepository _parkmeterRepository;

        public GetParkmetersByStatusQueryHandler(
            IParkmeterRepository parkmeterRepository)
        {
            _parkmeterRepository = parkmeterRepository;
        }

        public async Task<List<Parkmeter>> Handle(
            GetParkmetersByStatusQuery request,
            CancellationToken ct)
        {
            var parkmeters = await _parkmeterRepository
                .GetByStatusAsync(request.Status);
            return parkmeters.ToList();
        }
    }
}