using MediatR;

namespace parkmeter_management.Application.Commands.MachineEvents.HandleParkmeterOnline
{
    public class HandleParkmeterOnlineCommand: IRequest<bool>
    {
        public Guid ParkmeterId { get; set; }
    }
}