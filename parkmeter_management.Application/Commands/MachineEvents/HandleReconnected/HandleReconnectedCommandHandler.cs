using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Application.Interfaces.Services;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Application.Commands.MachineEvents.HandleReconnected
{
    public class HandleReconnectedCommandHandler
        : IRequestHandler<HandleReconnectedCommand, bool>
    {
        private readonly IAlertRepository _alertRepository;
        private readonly IParkmeterRepository _parkmeterRepository;
        private readonly IMessagePublisherService _mqttPublisher;
        private readonly INotificationService _notificationService;

        public HandleReconnectedCommandHandler(
            IAlertRepository alertRepository,
            IParkmeterRepository parkmeterRepository,
            IMessagePublisherService mqttPublisher,
            INotificationService notificationService)
        {
            _alertRepository     = alertRepository;
            _parkmeterRepository = parkmeterRepository;
            _mqttPublisher       = mqttPublisher;
            _notificationService = notificationService;
        }

        public async Task<bool> Handle(
            HandleReconnectedCommand command,
            CancellationToken ct)
        {

            // auto-resolve CONNECTION_LOST alert
            var alerts = await _alertRepository.GetByParkmeterAsync(command.ParkmeterId);
            foreach (var a in alerts.Where(a =>
                a.Status == AlertStatus.NOT_RESOLVED &&
                a.Type == AlertType.CONNECTION_LOST))
            {
                a.Status     = AlertStatus.RESOLVED;
                a.ResolvedAt = DateTime.UtcNow;
                await _alertRepository.UpdateAsync(a);
                Console.WriteLine("Auto-resolved on reconnect: CONNECTION_LOST");
            }

            // FIX: check if the meter is still blocked by other alerts.
            // If not, restore IN_SERVICE. Previously the status was never restored here.
            var remaining = await _alertRepository.GetByParkmeterAsync(command.ParkmeterId);
            var stillBlocked = remaining.Any(a =>
                a.Status == AlertStatus.NOT_RESOLVED &&
                (a.Type == AlertType.CASH_BOX_FULL  ||
                 a.Type == AlertType.PAPER_EMPTY     ||
                 a.Type == AlertType.DOOR_OPEN       ||
                 a.Type == AlertType.HARDWARE_FAILURE));

            if (!stillBlocked)
            {
                var parkmeter = await _parkmeterRepository.GetByIdAsync(command.ParkmeterId);
                if (parkmeter != null &&
                    parkmeter.Status == MeterStatus.TEMPORARILY_UNAVAILABLE)
                {
                    parkmeter.Status = MeterStatus.IN_SERVICE;
                    await _parkmeterRepository.UpdateAsync(parkmeter);
                    await _mqttPublisher.PublishStatusAsync(
                        command.ParkmeterId,
                        MeterStatus.IN_SERVICE.ToString());
                    await _notificationService.NotifyStatusChanged(
                        command.ParkmeterId,
                        MeterStatus.IN_SERVICE.ToString());
                    Console.WriteLine("Reconnected — meter restored to IN_SERVICE");
                }
            }

            return true;
        }
    }
}