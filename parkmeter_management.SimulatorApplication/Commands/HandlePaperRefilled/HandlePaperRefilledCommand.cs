using MediatR;

namespace parkmeter_management.SimulatorApplication.Commands.HandlePaperRefilled
{
    public class HandlePaperRefilledCommand : IRequest<bool>
    {
        public Guid ParkmeterId { get; set; } 
    }
}