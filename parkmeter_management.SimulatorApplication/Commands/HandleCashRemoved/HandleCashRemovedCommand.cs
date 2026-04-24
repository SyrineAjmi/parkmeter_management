using MediatR;

namespace parkmeter_management.SimulatorApplication.Commands.HandleCashRemoved
{
    public class HandleCashRemovedCommand : IRequest<bool>
    {
        public Guid ParkmeterId { get; set; } 
    }

}