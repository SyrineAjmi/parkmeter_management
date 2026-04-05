using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Application.Queries.Parkmeters
{
    public class GetAllParkmetersQueryHandler
        : IRequestHandler<GetAllParkmetersQuery, List<Parkmeter>>
    {
        private readonly IParkmeterRepository _parkmeterRepository;

        public GetAllParkmetersQueryHandler(
            IParkmeterRepository parkmeterRepository)
        {
            _parkmeterRepository = parkmeterRepository;
        }

        public async Task<List<Parkmeter>> Handle(
            GetAllParkmetersQuery request, CancellationToken ct)
        {
            var parkmeters = await _parkmeterRepository.GetAllAsync();
            return parkmeters.ToList();
        }
    }
}