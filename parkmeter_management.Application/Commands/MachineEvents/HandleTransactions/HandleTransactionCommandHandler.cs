using MediatR;
using parkmeter_management.Application.Commands.Transactions;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Application.Commands.MachineEvents.HandleTransactions
{
    public class HandleTransactionCommandHandler
        : IRequestHandler<HandleTransactionCommand, bool>
    {
        private readonly IMediator _mediator;

        public HandleTransactionCommandHandler(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task<bool> Handle(
            HandleTransactionCommand command,
            CancellationToken ct)
        {
            Enum.TryParse<TransactionStatus>(
                command.Status, out var status);

            var transactionId = await _mediator.Send(
                new CreateTransactionCommand
                {
                    ParkmeterId = command.ParkmeterId,
                    Amount = command.Amount,
                    Duration = command.Duration,
                    Status = status
                }, ct);

            Console.WriteLine(
                $"Transaction saved: {status} | " +
                $"{command.Duration} min | {command.Amount} DT " +
                $"→ {transactionId}");

            return true;
        }
    }
}