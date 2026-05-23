using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Application.Interfaces.Services;
using parkmeter_management.Domain.Entities;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Application.Commands.MachineEvents.HandleDoorClosed
{
    public class HandleDoorClosedCommandHandler
        : IRequestHandler<HandleDoorClosedCommand, bool>
    {
        private readonly IAlertRepository _alertRepository;
        private readonly INotificationService _notificationService;
        private readonly IParkmeterRepository _parkmeterRepository; 


        public HandleDoorClosedCommandHandler(
            IAlertRepository alertRepository,
            INotificationService notificationService,
            IParkmeterRepository parkmeterRepository)
        {
            _alertRepository = alertRepository;
            _notificationService = notificationService;
            _parkmeterRepository = parkmeterRepository;
        }

        public async Task<bool> Handle(
            HandleDoorClosedCommand command,
            CancellationToken ct)
        {
            // auto-resolve DOOR_OPEN alert
            var alerts = await _alertRepository
                .GetByParkmeterAsync(command.ParkmeterId);
            var parkmeter = await _parkmeterRepository.GetByIdAsync(command.ParkmeterId); 

            foreach (var a in alerts.Where(a =>
                a.Status == AlertStatus.NOT_RESOLVED &&
                a.Type == AlertType.DOOR_OPEN))
            {
                a.Status = AlertStatus.RESOLVED;
                a.ResolvedAt = DateTime.UtcNow;
                await _alertRepository.UpdateAsync(a);
                await _notificationService.NotifyAlertResolved(  // ← add after UpdateAsync
                    a.Id,
                    parkmeter?.SerialNumber ?? string.Empty,
                    a.ParkmeterId);

                Console.WriteLine("Auto-resolved: DOOR_OPEN");
            }

            // no status push — machine will report IN_SERVICE itself
            Console.WriteLine(
                $"[DOOR_CLOSED] processed — " +
                $"machine will report its own status");

            return true;
        }
    }
}
