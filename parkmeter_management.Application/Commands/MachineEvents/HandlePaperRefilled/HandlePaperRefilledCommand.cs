using MediatR;

namespace parkmeter_management.Application.Commands.MachineEvents.HandlePaperRefilled
{
    public class HandlePaperRefilledCommand : IRequest<bool>
    {
        public Guid ParkmeterId { get; set; }
    }
}