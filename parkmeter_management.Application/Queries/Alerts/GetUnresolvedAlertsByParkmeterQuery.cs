using MediatR;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Application.Queries.Alerts
{
    public class GetUnresolvedAlertsByParkmeterQuery
        : IRequest<List<Alert>>
    {
        public Guid ParkmeterID { get; set; }
    }
}