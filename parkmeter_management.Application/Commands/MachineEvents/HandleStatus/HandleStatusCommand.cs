using MediatR;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Application.Commands.MachineEvents.HandleStatus
{
    public class HandleStatusCommand : IRequest<bool>
    {
        public Guid ParkmeterId { get; set; }
        public MeterStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}