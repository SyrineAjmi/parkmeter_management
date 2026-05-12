using MediatR;

namespace parkmeter_management.Application.Commands.MachineEvents.HandleAlerts
{
    public class HandleAlertCommand : IRequest<bool>
    {
        public Guid ParkmeterId { get; set; }
        public int AlertType { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}