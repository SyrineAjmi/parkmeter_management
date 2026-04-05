using MediatR;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Application.Commands.Parkmeters
{
    public class ChangeStatusCommand : IRequest<bool>
    {
        public Guid ParkmeterId { get; set; }
        public MeterStatus NewStatus { get; set; }
        
    }
    
}
