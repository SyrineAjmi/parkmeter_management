using MediatR;

namespace parkmeter_management.Application.Commands.Alerts
{
    public class ResolveAlertCommand : IRequest<bool>
    {
        public Guid AlertId { get; set; }
    }
}