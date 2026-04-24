using MediatR;
using parkmeter_management.Application.Helpers;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Application.Interfaces.Services;
using parkmeter_management.Domain.Entities;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Application.Commands.Alerts
{
    public class CreateAlertCommandHandler
        : IRequestHandler<CreateAlertCommand, Guid>
    {
        private readonly IAlertRepository _alertRepository;
        private readonly IParkmeterRepository _parkmeterRepository;
        // FIX #1 — removed unused ISessionRepository
        private readonly IMqttPublisherService _mqttPublisher;
        private readonly INotificationService _notificationService;

        public CreateAlertCommandHandler(
            IAlertRepository alertRepository,
            IParkmeterRepository parkmeterRepository,
            // FIX #1 — removed unused ISessionRepository parameter
            IMqttPublisherService mqttPublisher,
            INotificationService notificationService)
        {
            _alertRepository = alertRepository;
            _parkmeterRepository = parkmeterRepository;
            _mqttPublisher = mqttPublisher;
            _notificationService = notificationService;
        }

        public async Task<Guid> Handle(
            CreateAlertCommand command, CancellationToken ct)
        {
            // get parkmeter
            var parkmeter = await _parkmeterRepository
                .GetByIdAsync(command.ParkmeterId);
            if (parkmeter == null) return Guid.Empty;

            // check duplicates
            var existingAlerts = await _alertRepository
                .GetByParkmeterAsync(command.ParkmeterId);
            var isDuplicate = existingAlerts.Any(a =>
                a.Type == command.Type &&
                a.Status == AlertStatus.NOT_RESOLVED);
            if (isDuplicate) return Guid.Empty;

            // operational alerts only valid when IN_SERVICE or TEMPORARILY_UNAVAILABLE
            var operationalAlerts =
                AlertType.CASH_BOX_WARN |
                AlertType.CASH_BOX_FULL |
                AlertType.PAPER_LOW |
                AlertType.PAPER_EMPTY;

            if ((command.Type & operationalAlerts) != AlertType.NONE
                && parkmeter.Status != MeterStatus.IN_SERVICE
                && parkmeter.Status != MeterStatus.TEMPORARILY_UNAVAILABLE)
                return Guid.Empty;

            // blocking alerts → TEMPORARILY_UNAVAILABLE
            var blockingAlerts =
                AlertType.CASH_BOX_FULL |
                AlertType.PAPER_EMPTY |
                AlertType.DOOR_OPEN |
                AlertType.HARDWARE_FAILURE;

            if ((command.Type & blockingAlerts) != AlertType.NONE
                && parkmeter.Status == MeterStatus.IN_SERVICE)
            {
                parkmeter.Status = MeterStatus.TEMPORARILY_UNAVAILABLE;
                await _parkmeterRepository.UpdateAsync(parkmeter);
                await _mqttPublisher.PublishStatusAsync(
                    command.ParkmeterId,
                    MeterStatus.TEMPORARILY_UNAVAILABLE.ToString());
            }

            // POWER_FAILURE → OUT_OF_SERVICE
            if (command.Type.HasFlag(AlertType.POWER_FAILURE))
            {
                parkmeter.Status = MeterStatus.OUT_OF_SERVICE;
                await _parkmeterRepository.UpdateAsync(parkmeter);
                await _mqttPublisher.PublishStatusAsync(
                    command.ParkmeterId,
                    MeterStatus.OUT_OF_SERVICE.ToString());
            }

            // save alert
            var alert = new Alert
            {
                Id = Guid.NewGuid(),
                ParkmeterId = command.ParkmeterId,
                Type = command.Type,
                Priority = AlertPriorityHelper.GetPriority(command.Type),
                Description = command.Description,
                Status = AlertStatus.NOT_RESOLVED,
                CreatedAt = DateTime.UtcNow
            };

            await _alertRepository.AddAsync(alert);
            await _notificationService.NotifyNewAlert(
                command.ParkmeterId,
                command.Type.ToString(),
                AlertPriorityHelper.GetPriority(command.Type).ToString());

            return alert.Id;
        }
    }
}