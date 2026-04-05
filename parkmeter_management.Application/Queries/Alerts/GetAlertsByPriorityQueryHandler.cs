using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Domain.Entities;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Application.Queries.Alerts
{
    public class GetAlertsByPriorityQueryHandler
        : IRequestHandler<GetAlertsByPriorityQuery, List<Alert>>
    {
        private readonly IAlertRepository _alertRepository;

        public GetAlertsByPriorityQueryHandler(
            IAlertRepository alertRepository)
        {
            _alertRepository = alertRepository;
        }

        public async Task<List<Alert>> Handle(
            GetAlertsByPriorityQuery request, CancellationToken ct)
        {
            if (!Enum.TryParse<Priority>(request.Priority, true, out var priority))
                return new List<Alert>();

            var alerts = await _alertRepository.GetByPriorityAsync(priority);
            return alerts.ToList();
        }
    }
}