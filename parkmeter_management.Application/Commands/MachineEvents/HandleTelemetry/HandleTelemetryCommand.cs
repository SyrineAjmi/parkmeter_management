using MediatR;

namespace parkmeter_management.Application.Commands.MachineEvents.HandleTelemetry
{
    public class HandleTelemetryCommand : IRequest<bool>
    {
        public Guid ParkmeterId { get; set; }
        public decimal CashAmount { get; set; }
        public int TransactionCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}