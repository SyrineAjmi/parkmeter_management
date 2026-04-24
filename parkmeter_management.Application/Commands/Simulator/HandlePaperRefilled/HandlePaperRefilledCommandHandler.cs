using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Application.Interfaces.Services;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Application.Commands.Simulator.HandlePaperRefilled
{
    public class HandlePaperRefilledCommandHandler
        : IRequestHandler<HandlePaperRefilledCommand, bool>
    {
        private readonly IAlertRepository _alertRepository;
        private readonly IParkmeterRepository _parkmeterRepository;
        private readonly IInterventionOrderRepository _interventionOrderRepository;
        private readonly IMqttPublisherService _mqttPublisher;
        private readonly INotificationService _notificationService;

        public HandlePaperRefilledCommandHandler(
            IAlertRepository alertRepository,
            IParkmeterRepository parkmeterRepository,
            IInterventionOrderRepository interventionOrderRepository,
            IMqttPublisherService mqttPublisher,
            INotificationService notificationService)
        {
            _alertRepository = alertRepository;
            _parkmeterRepository = parkmeterRepository;
            _interventionOrderRepository = interventionOrderRepository;
            _mqttPublisher = mqttPublisher;
            _notificationService = notificationService;
        }

        public async Task<bool> Handle(
            HandlePaperRefilledCommand command,
            CancellationToken ct)
        {
            var parkmeterID = command.ParkmeterId;

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

                await _notificationService.NotifyAlertResolved(
                    a.Id, parkmeterID);

                Console.WriteLine($"Auto-resolved: {a.Type}");
            }

            // auto-complete intervention order
            var order = await _interventionOrderRepository
                .GetPendingByParkmeterAndTypeAsync(
                    parkmeterID,
                    InterventionType.PAPER_REFILL);

            if (order != null)
            {
                order.Status = InterventionOrderStatus.COMPLETED;
                order.CompletedAt = DateTime.UtcNow;
                await _interventionOrderRepository.UpdateAsync(order);

                await _notificationService.NotifyInterventionOrderCompleted(
                    order.Id,
                    parkmeterID,
                    order.CompletedAt.Value);

                Console.WriteLine("✅ Paper refill order completed");
            }

            // check stillBlocked
            var remaining = await _alertRepository
                .GetByParkmeterAsync(parkmeterID);

            var stillBlocked = remaining.Any(a =>
                a.Status == AlertStatus.NOT_RESOLVED &&
                (a.Type == AlertType.CASH_BOX_FULL ||
                 a.Type == AlertType.HARDWARE_FAILURE ||
                 a.Type == AlertType.DOOR_OPEN));

            var otherOrder = await _interventionOrderRepository
                .GetPendingByParkmeterAndTypeAsync(
                    parkmeterID,
                    InterventionType.CASH_COLLECTION);

            var parkmeter = await _parkmeterRepository
                .GetByIdAsync(parkmeterID);

            if (!stillBlocked &&
                parkmeter != null &&
                otherOrder == null &&
                (parkmeter.Status == MeterStatus.TEMPORARILY_UNAVAILABLE ||
                 parkmeter.Status == MeterStatus.MAINTENANCE))
            {
                parkmeter.Status = MeterStatus.IN_SERVICE;
                await _parkmeterRepository.UpdateAsync(parkmeter);
                await _mqttPublisher.PublishStatusAsync(
                    parkmeterID,
                    MeterStatus.IN_SERVICE.ToString());

                await _notificationService.NotifyStatusChanged(
                    parkmeterID,
                    MeterStatus.IN_SERVICE.ToString());

                Console.WriteLine("✅ IN_SERVICE");
            }

            return true;
        }
    }
}