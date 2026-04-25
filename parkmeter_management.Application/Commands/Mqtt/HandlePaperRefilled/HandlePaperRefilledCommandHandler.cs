using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Application.Interfaces.Services;
using parkmeter_management.Domain.Entities;
using parkmeter_management.Domain.Enums;
using System.Text.Json;

namespace parkmeter_management.Application.Commands.Mqtt.HandlePaperRefilled
{
    public class HandlePaperRefilledCommandHandler
        : IRequestHandler<HandlePaperRefilledCommand, bool>
    {
        private readonly IAlertRepository _alertRepository;
        private readonly IParkmeterRepository _parkmeterRepository;
        private readonly IMqttPublisherService _mqttPublisher;
        private readonly IInterventionOrderRepository _interventionOrderRepository; // ← add
        private readonly INotificationService _notificationService; // ← add

        public HandlePaperRefilledCommandHandler(
            IAlertRepository alertRepository,
            IParkmeterRepository parkmeterRepository,
            IMqttPublisherService mqttPublisher,
            IInterventionOrderRepository interventionOrderRepository, // ← add
            INotificationService notificationService) // ← add
        {
            _alertRepository = alertRepository;
            _parkmeterRepository = parkmeterRepository;
            _mqttPublisher = mqttPublisher;
            _interventionOrderRepository = interventionOrderRepository; // ← add
            _notificationService = notificationService; // ← add
        }

        public async Task<bool> Handle(
            HandlePaperRefilledCommand command,
            CancellationToken ct)
        {
            var raw = JsonSerializer
                .Deserialize<JsonElement>(command.Payload);
            var parkmeterID = Guid.Parse(
                raw.GetProperty("ParkmeterId").GetString()!);

            // auto-resolve paper alerts
            var alerts = await _alertRepository
                .GetByParkmeterAsync(parkmeterID);
            foreach (var a in alerts.Where(a =>
                a.Status == AlertStatus.NOT_RESOLVED &&
                (a.Type == AlertType.PAPER_EMPTY ||
                 a.Type == AlertType.PAPER_LOW)))
            {
                a.Status = AlertStatus.RESOLVED;
                a.ResolvedAt = DateTime.UtcNow;
                await _alertRepository.UpdateAsync(a);
                Console.WriteLine($"Auto-resolved: {a.Type}");
            }

            // auto-complete intervention order ← ADD
            var pendingOrder = await _interventionOrderRepository
                .GetPendingByParkmeterAndTypeAsync(
                    parkmeterID,
                    InterventionType.PAPER_REFILL);

            if (pendingOrder != null)
            {
                pendingOrder.Status = InterventionOrderStatus.COMPLETED;
                pendingOrder.CompletedAt = DateTime.UtcNow;
                await _interventionOrderRepository
                    .UpdateAsync(pendingOrder);
                Console.WriteLine($"✅ Paper refill order completed");

                await _notificationService
                    .NotifyInterventionOrderCompleted(
                        pendingOrder.Id,
                        parkmeterID,
                        pendingOrder.CompletedAt.Value);
            }

            // check stillBlocked
            var remaining = await _alertRepository
                .GetByParkmeterAsync(parkmeterID);
            var stillBlocked = remaining.Any(a =>
                a.Status == AlertStatus.NOT_RESOLVED &&
                (a.Type == AlertType.CASH_BOX_FULL ||
                 a.Type == AlertType.HARDWARE_FAILURE ||
                 a.Type == AlertType.DOOR_OPEN));

            // revert to IN_SERVICE ← update condition
            var parkmeter = await _parkmeterRepository
                .GetByIdAsync(parkmeterID);

            // Check if cash collection order pending:
            var anyOtherPendingOrder = await _interventionOrderRepository
                .GetPendingByParkmeterAndTypeAsync(
                    parkmeterID,
                    InterventionType.CASH_COLLECTION); // check cash order

            if (!stillBlocked && parkmeter != null &&
                (parkmeter.Status ==
                    MeterStatus.TEMPORARILY_UNAVAILABLE ||
                 parkmeter.Status ==
                    MeterStatus.MAINTENANCE) &&
                 anyOtherPendingOrder == null) // only if no other pending ✅
            {
                parkmeter.Status = MeterStatus.IN_SERVICE;
                await _parkmeterRepository.UpdateAsync(parkmeter);
                await _mqttPublisher.PublishStatusAsync(
                    parkmeterID,
                    MeterStatus.IN_SERVICE.ToString());
                Console.WriteLine(
                    $"{parkmeter.SerialNumber} : IN_SERVICE");
            }

            // save resolved alert
            await _alertRepository.AddAsync(new Alert
            {
                Id = Guid.NewGuid(),
                ParkmeterId = parkmeterID,
                Type = AlertType.PAPER_REFILLED,
                Priority = Priority.LOW,
                Description = "Paper refilled by agent",
                Status = AlertStatus.RESOLVED,
                CreatedAt = DateTime.UtcNow,
                ResolvedAt = DateTime.UtcNow
            });

            return true;
        }
    }
}