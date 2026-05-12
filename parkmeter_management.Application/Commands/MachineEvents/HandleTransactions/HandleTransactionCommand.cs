using MediatR;

namespace parkmeter_management.Application.Commands.MachineEvents.HandleTransactions
{
    public class HandleTransactionCommand : IRequest<bool>
    {
        public Guid ParkmeterId { get; set; }
        public decimal Amount { get; set; }
        public int Duration { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}