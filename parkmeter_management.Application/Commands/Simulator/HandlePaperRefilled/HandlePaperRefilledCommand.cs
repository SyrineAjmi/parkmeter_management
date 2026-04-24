using MediatR;

namespace parkmeter_management.Application.Commands.Simulator.HandlePaperRefilled
{
    public class HandlePaperRefilledCommand : IRequest<bool>
    {
        public Guid ParkmeterId { get; set; } 
    }
}