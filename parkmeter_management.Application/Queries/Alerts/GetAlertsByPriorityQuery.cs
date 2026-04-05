using MediatR;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Application.Queries.Alerts
{
    public class GetAlertsByPriorityQuery : IRequest<List<Alert>>
    {
        public string Priority { get; set; } = string.Empty;
    }
}