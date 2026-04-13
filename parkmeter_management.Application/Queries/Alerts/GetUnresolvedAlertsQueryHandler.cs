using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Application.Queries.Alerts
{
    public class GetUnresolvedAlertsQueryHandler
        : IRequestHandler<GetUnresolvedAlertsQuery, List<Alert>>
    {
        private readonly IAlertRepository _alertRepository;

        public GetUnresolvedAlertsQueryHandler(
            IAlertRepository alertRepository)
        {
            _alertRepository = alertRepository;
        }

        public async Task<List<Alert>> Handle(
            GetUnresolvedAlertsQuery request,
            CancellationToken ct)
        {
            var alerts = await _alertRepository
                .GetUnresolvedAsync(); 
            return alerts.ToList();
        }
    }
}