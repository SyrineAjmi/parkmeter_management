using MediatR;

namespace parkmeter_management.Application.Commands.Transactions

{

using parkmeter_management.Domain.Enums;

public class CreateTransactionCommand : IRequest<Guid>
    {
        public Guid ParkmeterId { get; set; }
        public decimal Amount { get; set; }
        public int Duration { get; set; }
        public TransactionStatus Status { get; set; } 

    }
}