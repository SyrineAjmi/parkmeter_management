using MediatR;

namespace parkmeter_management.Application.Commands.MachineEvents.HandleHardwareFixed
{
    public class HandleHardwareFixedCommand : IRequest<bool>
    {
        public Guid ParkmeterId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}