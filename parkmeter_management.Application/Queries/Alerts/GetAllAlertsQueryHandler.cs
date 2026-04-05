using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Application.Queries.Alerts
{
    public class GetAllAlertsQueryHandler
        : IRequestHandler<GetAllAlertsQuery, List<Alert>>
    {
        private readonly IAlertRepository _alertRepository;

        public GetAllAlertsQueryHandler(IAlertRepository alertRepository)
        {
            _alertRepository = alertRepository;
        }

        public async Task<List<Alert>> Handle(
            GetAllAlertsQuery request, CancellationToken ct)
        {
            var alerts = await _alertRepository.GetAllAsync();
            return alerts.ToList();
        }
    }
}