using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Api.Requests.InterventionOrder
{
    public class CreateInterventionOrderRequest
    {
        public Guid ParkmeterId { get; set; }
        public Guid OperatorId { get; set; }
        public InterventionType Type { get; set; }
            = InterventionType.CASH_COLLECTION;
    }
}