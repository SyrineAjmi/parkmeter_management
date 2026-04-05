using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Application.Queries.Parkmeters
{
    public class GetParkmeterByIdQueryHandler
        : IRequestHandler<GetParkmeterByIdQuery, Parkmeter?>
    {
        private readonly IParkmeterRepository _parkmeterRepository;

        public GetParkmeterByIdQueryHandler(
            IParkmeterRepository parkmeterRepository)
        {
            _parkmeterRepository = parkmeterRepository;
        }

        public async Task<Parkmeter?> Handle(
            GetParkmeterByIdQuery request, CancellationToken ct)
        {
            return await _parkmeterRepository.GetByIdAsync(request.Id);
        }
    }
}