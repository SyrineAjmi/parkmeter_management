using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Application.Interfaces.Services;
using parkmeter_management.Domain.Entities;
using parkmeter_management.Domain.Enums;


namespace parkmeter_management.Application.Commands.MachineEvents.HandleParkmeterOffline
{
    public class HandleParkmeterOfflineCommandHandler
        : IRequestHandler<HandleParkmeterOfflineCommand, bool>
    {
        private readonly IParkmeterRepository _parkmeterRepository;
        private readonly IAlertRepository _alertRepository;
        private readonly IMessagePublisherService _mqttPublisher;
        private readonly INotificationService _notificationService;

        public HandleParkmeterOfflineCommandHandler(
            IParkmeterRepository parkmeterRepository,
            IAlertRepository alertRepository,
            IMessagePublisherService mqttPublisher,
            INotificationService notificationService)
        {
            _parkmeterRepository = parkmeterRepository;
            _alertRepository = alertRepository;
            _mqttPublisher = mqttPublisher;
            _notificationService = notificationService;
        }

        public async Task<bool> Handle(
            HandleParkmeterOfflineCommand command,
            CancellationToken ct)
        {
            var parkmeter = await _parkmeterRepository
                .GetByIdAsync(command.ParkmeterId);
            if (parkmeter == null) return false;

            // deduplicate — don't raise if already open
            var existing = await _alertRepository
                .GetByParkmeterAsync(command.ParkmeterId);
            var alreadyRaised = existing.Any(a =>
                a.Type == AlertType.POWER_FAILURE &&
                a.Status == AlertStatus.NOT_RESOLVED);

            if (alreadyRaised) return true;

            await _alertRepository.AddAsync(new Alert
            {
                Id = Guid.NewGuid(),
                ParkmeterId = command.ParkmeterId,
                Type = AlertType.POWER_FAILURE,
                Priority = Priority.HIGH,
                Description = "Machine reported power failure before going offline",
                Status = AlertStatus.NOT_RESOLVED,
                CreatedAt = DateTime.UtcNow
            });

            parkmeter.Status = MeterStatus.OUT_OF_SERVICE;
            await _parkmeterRepository.UpdateAsync(parkmeter);

            await _mqttPublisher.PublishStatusAsync(
                command.ParkmeterId,
                MeterStatus.OUT_OF_SERVICE.ToString());

            await _notificationService.NotifyNewAlert(
                command.ParkmeterId,
                AlertType.POWER_FAILURE.ToString(),
                Priority.HIGH.ToString());

            Console.WriteLine(
                $"[OFFLINE] {parkmeter.SerialNumber} → OUT_OF_SERVICE");

            return true;
        }
    }
}