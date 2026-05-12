using MediatR;

namespace parkmeter_management.Application.Commands.MachineEvents.HandleReconnected
{
    public class HandleReconnectedCommand : IRequest<bool>
    {
        public Guid ParkmeterId { get; set; }
    }
}