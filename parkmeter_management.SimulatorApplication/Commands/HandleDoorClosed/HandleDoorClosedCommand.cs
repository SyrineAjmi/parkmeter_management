using MediatR;

namespace parkmeter_management.SimulatorApplication.Commands.HandleDoorClosed
{
    public class HandleDoorClosedCommand : IRequest<bool>
    {
        public Guid ParkmeterId { get; set; } 
    }
}