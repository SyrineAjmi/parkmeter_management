using MediatR;

namespace parkmeter_management.Application.Commands.MachineEvents.HandleCashRemoved
{
    public class HandleCashRemovedCommand : IRequest<bool>
    {
        public Guid ParkmeterId { get; set; }
    }

}