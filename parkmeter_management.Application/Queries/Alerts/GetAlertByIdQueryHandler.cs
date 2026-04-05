using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Application.Queries.Alerts
{
    public class GetAlertByIdQueryHandler
        : IRequestHandler<GetAlertByIdQuery, Alert?>
    {
        private readonly IAlertRepository _alertRepository;

        public GetAlertByIdQueryHandler(IAlertRepository alertRepository)
        {
            _alertRepository = alertRepository;
        }

        public async Task<Alert?> Handle(
            GetAlertByIdQuery request, CancellationToken ct)
        {
            return await _alertRepository.GetByIdAsync(request.Id);
        }
    }
}