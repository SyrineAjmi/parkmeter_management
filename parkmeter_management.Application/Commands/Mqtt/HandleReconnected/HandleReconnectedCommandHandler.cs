using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Application.Interfaces.Services;
using parkmeter_management.Domain.Enums;
using System.Text.Json;

namespace parkmeter_management.Application.Commands.Mqtt.HandleReconnected
{
    public class HandleReconnectedCommandHandler
        : IRequestHandler<HandleReconnectedCommand, bool>
    {
        private readonly IAlertRepository _alertRepository;
        private readonly IParkmeterRepository _parkmeterRepository;       // FIX #3
        private readonly IMqttPublisherService _mqttPublisher;            // FIX #3
        private readonly INotificationService _notificationService;       // FIX #4

        public HandleReconnectedCommandHandler(
            IAlertRepository alertRepository,
            IParkmeterRepository parkmeterRepository,                     // FIX #3
            IMqttPublisherService mqttPublisher,                          // FIX #3
            INotificationService notificationService)                     // FIX #4
        {
            _alertRepository = alertRepository;
            _parkmeterRepository = parkmeterRepository;                   // FIX #3
            _mqttPublisher = mqttPublisher;                               // FIX #3
            _notificationService = notificationService;                   // FIX #4
        }

        public async Task<bool> Handle(
            HandleReconnectedCommand command,
            CancellationToken ct)
        {
            var raw = JsonSerializer
                .Deserialize<JsonElement>(command.Payload);
            var parkmeterID = Guid.Parse(
                raw.GetProperty("ParkmeterId").GetString()!);

            // auto-resolve CONNECTION_LOST alert
            var alerts = await _alertRepository
                .GetByParkmeterAsync(parkmeterID);

            foreach (var a in alerts.Where(a =>
                a.Status == AlertStatus.NOT_RESOLVED &&
                a.Type == AlertType.CONNECTION_LOST))
            {
                a.Status = AlertStatus.RESOLVED;
                a.ResolvedAt = DateTime.UtcNow;
                await _alertRepository.UpdateAsync(a);

                // FIX #4 — notify frontend the alert is resolved
                await _notificationService.NotifyAlertResolved(
                    a.Id, parkmeterID);

                Console.WriteLine("Auto-resolved on reconnect: CONNECTION_LOST");
            }

            // FIX #3 — restore parkmeter status to IN_SERVICE if it was
            // OUT_OF_SERVICE due to connection loss and no other blocking
            // alerts remain
            var parkmeter = await _parkmeterRepository
                .GetByIdAsync(parkmeterID);

            if (parkmeter == null) return false;

            // only restore if the machine went down due to connection loss
            // not if it's in MAINTENANCE or TEMPORARILY_UNAVAILABLE for
            // other reasons (cash full, paper empty etc.)
            if (parkmeter.Status == MeterStatus.OUT_OF_SERVICE)
            {
                var remaining = await _alertRepository
                    .GetByParkmeterAsync(parkmeterID);

                var stillBlocked = remaining.Any(a =>
                    a.Status == AlertStatus.NOT_RESOLVED &&
                    (a.Type == AlertType.CASH_BOX_FULL ||
                     a.Type == AlertType.PAPER_EMPTY ||
                     a.Type == AlertType.DOOR_OPEN ||
                     a.Type == AlertType.HARDWARE_FAILURE ||
                     a.Type == AlertType.POWER_FAILURE));

                if (!stillBlocked)
                {
                    parkmeter.Status = MeterStatus.IN_SERVICE;
                    await _parkmeterRepository.UpdateAsync(parkmeter);

                    await _mqttPublisher.PublishStatusAsync(
                        parkmeterID,
                        MeterStatus.IN_SERVICE.ToString());

                    // FIX #4 — notify frontend of status restore
                    await _notificationService.NotifyStatusChanged(
                        parkmeterID,
                        MeterStatus.IN_SERVICE.ToString());

                    Console.WriteLine(
                        $"[RECONNECTED] Parkmeter restored → IN_SERVICE");
                }
            }

            return true;
        }
    }
}