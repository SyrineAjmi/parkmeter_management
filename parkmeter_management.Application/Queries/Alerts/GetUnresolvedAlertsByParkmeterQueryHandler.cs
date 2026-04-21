using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Application.Queries.Alerts
{
    public class GetUnresolvedAlertsByParkmeterQueryHandler
        : IRequestHandler<GetUnresolvedAlertsByParkmeterQuery,
            List<Alert>>
    {
        private readonly IAlertRepository _alertRepository;

        public GetUnresolvedAlertsByParkmeterQueryHandler(
            IAlertRepository alertRepository)
        {
            _alertRepository = alertRepository;
        }

        public async Task<List<Alert>> Handle(
            GetUnresolvedAlertsByParkmeterQuery request,
            CancellationToken ct)
        {
            var alerts = await _alertRepository
                .GetUnresolvedByParkmeterAsync(request.ParkmeterID);
            return alerts.ToList();
        }
    }
}