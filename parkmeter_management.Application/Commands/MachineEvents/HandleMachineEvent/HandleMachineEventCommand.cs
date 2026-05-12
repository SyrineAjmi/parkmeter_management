using MediatR;

namespace parkmeter_management.Application.Commands.MachineEvents.HandleMachineEvent
{
    public class HandleMachineEventCommand : IRequest<bool>
    {
        public Guid ParkmeterId { get; set; }
        public int EventType { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}