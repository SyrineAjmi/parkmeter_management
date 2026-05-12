using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Application.Interfaces.Services;
using parkmeter_management.Domain.Entities;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Application.Commands.MachineEvents.HandlePaperRefilled
{
    public class HandlePaperRefilledCommandHandler
        : IRequestHandler<HandlePaperRefilledCommand, bool>
    {
        private readonly IAlertRepository _alertRepository;
        private readonly IParkmeterRepository _parkmeterRepository;
        private readonly IMessagePublisherService _mqttPublisher;
        private readonly IInterventionOrderRepository _interventionOrderRepository; 
        private readonly INotificationService _notificationService; 

        public HandlePaperRefilledCommandHandler(
            IAlertRepository alertRepository,
            IParkmeterRepository parkmeterRepository,
            IMessagePublisherService mqttPublisher,
            IInterventionOrderRepository interventionOrderRepository, 
            INotificationService notificationService) 
        {
            _alertRepository = alertRepository;
            _parkmeterRepository = parkmeterRepository;
            _mqttPublisher = mqttPublisher;
            _interventionOrderRepository = interventionOrderRepository; 
            _notificationService = notificationService; 
        }

        public async Task<bool> Handle(
            HandlePaperRefilledCommand command,
            CancellationToken ct)
        {


            // auto-resolve paper alerts
            var alerts = await _alertRepository
                .GetByParkmeterAsync(command.ParkmeterId);
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

            // auto-complete intervention order 
            var pendingOrder = await _interventionOrderRepository
                .GetPendingByParkmeterAndTypeAsync(
                    command.ParkmeterId,
                    InterventionType.PAPER_REFILL);

            if (pendingOrder != null)
            {
                pendingOrder.Status = InterventionOrderStatus.COMPLETED;
                pendingOrder.CompletedAt = DateTime.UtcNow;
                await _interventionOrderRepository
                    .UpdateAsync(pendingOrder);
                Console.WriteLine($"Paper refill order completed");

                await _notificationService
                    .NotifyInterventionOrderCompleted(
                        pendingOrder.Id,
                        command.ParkmeterId,
                        pendingOrder.CompletedAt.Value);
            }

            // check stillBlocked
            var remaining = await _alertRepository
                .GetByParkmeterAsync(command.ParkmeterId);
            var stillBlocked = remaining.Any(a =>
                a.Status == AlertStatus.NOT_RESOLVED &&
                (a.Type == AlertType.CASH_BOX_FULL ||
                 a.Type == AlertType.HARDWARE_FAILURE ||
                 a.Type == AlertType.DOOR_OPEN));

            // revert to IN_SERVICE ← update condition
            var parkmeter = await _parkmeterRepository
                .GetByIdAsync(command.ParkmeterId);

            // Check if cash collection order pending:
            var anyOtherPendingOrder = await _interventionOrderRepository
                .GetPendingByParkmeterAndTypeAsync(
                    command.ParkmeterId,
                    InterventionType.CASH_COLLECTION); // check cash order

            if (!stillBlocked && parkmeter != null &&
                (parkmeter.Status ==
                    MeterStatus.TEMPORARILY_UNAVAILABLE ||
                 parkmeter.Status ==
                    MeterStatus.MAINTENANCE) &&
                 anyOtherPendingOrder == null) // only if no other pending 
            {
                parkmeter.Status = MeterStatus.IN_SERVICE;
                await _parkmeterRepository.UpdateAsync(parkmeter);
                await _mqttPublisher.PublishStatusAsync(
                    command.ParkmeterId,
                    MeterStatus.IN_SERVICE.ToString());
                Console.WriteLine(
                    $"{parkmeter.SerialNumber} : IN_SERVICE");
            }


            return true;
        }
    }
}