using MediatR;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Application.Commands.Alerts
{
    public class CreateAlertCommand : IRequest<Guid>
    {
        public Guid ParkmeterId { get; set; }
        public AlertType Type { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}