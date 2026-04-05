using MediatR;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Application.Queries.Alerts
{
    public class GetAllAlertsQuery : IRequest<List<Alert>>
    {
    }
}