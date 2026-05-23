using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Application.Interfaces.Services;

namespace parkmeter_management.Application.Commands.MachineEvents.HandleTelemetry
{
    public class HandleTelemetryCommandHandler
            : IRequestHandler<HandleTelemetryCommand, bool>
    {
        private readonly IParkmeterRepository _parkmeterRepository;
        private readonly INotificationService _notificationService;

        public HandleTelemetryCommandHandler(
            IParkmeterRepository parkmeterRepository,
            INotificationService notificationService)
        {
            _parkmeterRepository = parkmeterRepository;
            _notificationService = notificationService;
        }

        public async Task<bool> Handle(
            HandleTelemetryCommand command,
            CancellationToken ct)
        {
            var parkmeter = await _parkmeterRepository
                .GetByIdAsync(command.ParkmeterId);
            if (parkmeter == null) return false;

            parkmeter.CashAmount = command.CashAmount;
            await _parkmeterRepository.UpdateAsync(parkmeter);

            await _notificationService.NotifyTelemetry(
                command.ParkmeterId,
                command.CashAmount,
                command.TransactionCount);

            Console.WriteLine(
                $"[TELEMETRY] {parkmeter.SerialNumber} " +
                $"| Cash: {command.CashAmount:F3} DT " +
                $"| Transactions: {command.TransactionCount}");

            return true;
        }
    }
}