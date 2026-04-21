using MediatR;
using parkmeter_management.Application.Commands.Transactions;
using parkmeter_management.Domain.Enums;
using System.Text.Json;

namespace parkmeter_management.Application.Commands.Mqtt.HandleTransactions
{
    public class HandleTransactionMqttCommandHandler
        : IRequestHandler<HandleTransactionMqttCommand, bool>
    {
        private readonly IMediator _mediator;

        public HandleTransactionMqttCommandHandler(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task<bool> Handle(
            HandleTransactionMqttCommand command,
            CancellationToken ct)
        {
            var raw = JsonSerializer
                .Deserialize<JsonElement>(command.Payload);

            var statusStr = raw.GetProperty("Status").GetString()!;
            Enum.TryParse<TransactionStatus>(statusStr, out var status);

            var transactionId = await _mediator.Send(
                new CreateTransactionCommand
                {
                    ParkmeterId = Guid.Parse(
                        raw.GetProperty("ParkmeterId").GetString()!),
                    Amount = raw.GetProperty("Amount").GetDecimal(),
                    Duration = raw.GetProperty("Duration").GetInt32(),
                    Status = status
                });

            Console.WriteLine(
                $"Transaction saved: {status} → {transactionId}");

            return true;
        }
    }
}