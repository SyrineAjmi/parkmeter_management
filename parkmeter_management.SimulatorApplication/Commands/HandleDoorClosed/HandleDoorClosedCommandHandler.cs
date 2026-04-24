using MediatR;
using parkmeter_management.Application
    .Interfaces.Repositories;
using parkmeter_management.Application
    .Interfaces.Services;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.SimulatorApplication
    .Commands.HandleDoorClosed
{
    public class HandleDoorClosedCommandHandler
        : IRequestHandler<HandleDoorClosedCommand, bool>
    {
        private readonly IAlertRepository _alertRepository;
        private readonly IParkmeterRepository _parkmeterRepository;
        private readonly IMqttPublisherService _mqttPublisher;
        private readonly INotificationService _notificationService; // FIX #2

        public HandleDoorClosedCommandHandler(
            IAlertRepository alertRepository,
            IParkmeterRepository parkmeterRepository,
            IMqttPublisherService mqttPublisher,
            INotificationService notificationService) // FIX #2
        {
            _alertRepository = alertRepository;
            _parkmeterRepository = parkmeterRepository;
            _mqttPublisher = mqttPublisher;
            _notificationService = notificationService; // FIX #2
        }

        public async Task<bool> Handle(
            HandleDoorClosedCommand command,
            CancellationToken ct)
        {
            var parkmeterID = command.ParkmeterId;

            // resolve door alert
            var alerts = await _alertRepository
                .GetByParkmeterAsync(parkmeterID);
            foreach (var a in alerts.Where(a =>
                a.Status == AlertStatus.NOT_RESOLVED &&
                a.Type == AlertType.DOOR_OPEN))
            {
                a.Status = AlertStatus.RESOLVED;
                a.ResolvedAt = DateTime.UtcNow;
                await _alertRepository.UpdateAsync(a);
                Console.WriteLine($"Auto-resolved: {a.Type}");

                // FIX #2 — notify frontend that the alert was resolved
                await _notificationService.NotifyAlertResolved(
                    a.Id,
                    parkmeterID);
            }

            // check stillBlocked
            var remaining = await _alertRepository
                .GetByParkmeterAsync(parkmeterID);
            var stillBlocked = remaining.Any(a =>
                a.Status == AlertStatus.NOT_RESOLVED &&
                (a.Type == AlertType.CASH_BOX_FULL ||
                 a.Type == AlertType.PAPER_EMPTY ||
                 a.Type == AlertType.HARDWARE_FAILURE));

            var parkmeter = await _parkmeterRepository
                .GetByIdAsync(parkmeterID);

            if (!stillBlocked &&
                parkmeter != null &&
                (parkmeter.Status ==
                    MeterStatus.TEMPORARILY_UNAVAILABLE ||
                 parkmeter.Status ==
                    MeterStatus.MAINTENANCE))
            {
                parkmeter.Status = MeterStatus.IN_SERVICE;
                await _parkmeterRepository.UpdateAsync(parkmeter);
                await _mqttPublisher.PublishStatusAsync(
                    parkmeterID,
                    MeterStatus.IN_SERVICE.ToString());

                // FIX #2 — notify frontend of the status change
                await _notificationService.NotifyStatusChanged(
                    parkmeterID,
                    MeterStatus.IN_SERVICE.ToString());

                Console.WriteLine("✅ IN_SERVICE");
            }

            return true;
        }
    }
}