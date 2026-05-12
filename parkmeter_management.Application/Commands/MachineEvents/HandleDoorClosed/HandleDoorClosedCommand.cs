using MediatR;

namespace parkmeter_management.Application.Commands.MachineEvents.HandleDoorClosed
{
    public class HandleDoorClosedCommand : IRequest<bool>
    {
        public Guid ParkmeterId { get; set; }
    }
}