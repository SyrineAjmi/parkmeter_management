using MediatR;

namespace parkmeter_management.Application.Commands.Simulator.HandleCashRemoved
{
    public class HandleCashRemovedCommand : IRequest<bool>
    {
        public Guid ParkmeterId { get; set; } 
    }

}