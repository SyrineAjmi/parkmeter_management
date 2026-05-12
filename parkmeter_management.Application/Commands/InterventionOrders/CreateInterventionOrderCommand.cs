using MediatR;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Application.Commands.InterventionOrders
{
    public class CreateInterventionOrderCommand : IRequest<Guid>
    {
        public Guid ParkmeterId { get; set; }
        public Guid OperatorId { get; set; }
        public InterventionType Type { get; set; }
    }
}