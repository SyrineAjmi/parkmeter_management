using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Application.Interfaces.Services;

namespace parkmeter_management.Application.Commands.MachineEvents.HandleStatus
{
    public class HandleStatusCommandHandler
        : IRequestHandler<HandleStatusCommand, bool>
    {
        private readonly IParkmeterRepository _parkmeterRepository;
        private readonly INotificationService _notificationService;

        public HandleStatusCommandHandler(
            IParkmeterRepository parkmeterRepository,
            INotificationService notificationService)
        {
            _parkmeterRepository = parkmeterRepository;
            _notificationService = notificationService;
        }

        public async Task<bool> Handle(
            HandleStatusCommand command,
            CancellationToken ct)
        {
            var parkmeter = await _parkmeterRepository
                .GetByIdAsync(command.ParkmeterId);
            if (parkmeter == null) return false;

            // skip if status unchanged
            if (parkmeter.Status == command.Status)
            {
                Console.WriteLine(
                    $"[STATUS] {parkmeter.SerialNumber} → {command.Status} " +
                    $"(unchanged — skipped)");
                return true;
            }

            parkmeter.Status = command.Status;
            await _parkmeterRepository.UpdateAsync(parkmeter);

            await _notificationService.NotifyStatusChanged(
                command.ParkmeterId,
                parkmeter.SerialNumber,
                command.Status.ToString());

            Console.WriteLine(
                $"[STATUS] {parkmeter.SerialNumber} " +
                $"→ {command.Status} (reported by machine)");

            return true;
        }
    }
}