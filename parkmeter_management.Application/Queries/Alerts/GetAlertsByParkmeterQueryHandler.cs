using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Application.Queries.Alerts
{
    public class GetAlertsByParkmeterQueryHandler
        : IRequestHandler<GetAlertsByParkmeterQuery, List<Alert>>
    {
        private readonly IAlertRepository _alertRepository;

        public GetAlertsByParkmeterQueryHandler(
            IAlertRepository alertRepository)
        {
            _alertRepository = alertRepository;
        }

        public async Task<List<Alert>> Handle(
            GetAlertsByParkmeterQuery request,
            CancellationToken ct)
        {
            var alerts = await _alertRepository
                .GetByParkmeterAsync(request.ParkmeterID);
            return alerts.ToList();
        }
    }
}