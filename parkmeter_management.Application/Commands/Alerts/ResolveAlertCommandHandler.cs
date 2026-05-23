using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Application.Interfaces.Services;
using parkmeter_management.Domain.Entities;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Application.Commands.Alerts
{
    public class ResolveAlertCommandHandler
        : IRequestHandler<ResolveAlertCommand, bool>
    {
        private readonly IAlertRepository _alertRepository;
        private readonly IInterventionOrderRepository _orderRepository;
        private readonly IParkmeterRepository _parkmeterRepository;
        private readonly IMessagePublisherService _mqttPublisher;
        private readonly INotificationService _notificationService;

        public ResolveAlertCommandHandler(
            IAlertRepository alertRepository,
            IInterventionOrderRepository orderRepository,
            IParkmeterRepository parkmeterRepository,
            IMessagePublisherService mqttPublisher,
            INotificationService notificationService)
        {
            _alertRepository = alertRepository;
            _orderRepository = orderRepository;
            _parkmeterRepository = parkmeterRepository;
            _mqttPublisher = mqttPublisher;
            _notificationService = notificationService;
        }

        public async Task<bool> Handle(
            ResolveAlertCommand request, CancellationToken ct)
        {
            var alert = await _alertRepository
                .GetByIdAsync(request.AlertId);
            if (alert == null) return false;

            // mark resolved
            alert.Status = AlertStatus.RESOLVED;
            alert.ResolvedAt = DateTime.UtcNow;
            await _alertRepository.UpdateAsync(alert);
            var parkmeter = await _parkmeterRepository.GetByIdAsync(alert.ParkmeterId);

            // notify dashboard
            await _notificationService.NotifyAlertResolved(
                request.AlertId,
                parkmeter?.SerialNumber ?? string.Empty,
                alert.ParkmeterId);

            Console.WriteLine(
                $"[ALERT] Resolved: {alert.Type} → {alert.Id}");

            // check remaining blocking alerts
            var remaining = await _alertRepository
                .GetByParkmeterAsync(alert.ParkmeterId);

            var stillBlocked = remaining.Any(a =>
                a.Status == AlertStatus.NOT_RESOLVED &&
                (a.Type == AlertType.CASH_BOX_FULL ||
                 a.Type == AlertType.PAPER_EMPTY ||
                 a.Type == AlertType.DOOR_OPEN ||
                 a.Type == AlertType.HARDWARE_FAILURE));

            // check pending intervention orders
            var pendingCashOrder = await _orderRepository
                .GetPendingByParkmeterAndTypeAsync(
                    alert.ParkmeterId,
                    InterventionType.CASH_COLLECTION);

            var pendingPaperOrder = await _orderRepository
                .GetPendingByParkmeterAndTypeAsync(
                    alert.ParkmeterId,
                    InterventionType.PAPER_REFILL);

            var pendingHardwareOrder = await _orderRepository
                .GetPendingByParkmeterAndTypeAsync(
                    alert.ParkmeterId,
                    InterventionType.HARDWARE_REPAIR);

            var hasPendingOrder =
                pendingCashOrder != null ||
                pendingPaperOrder != null ||
                pendingHardwareOrder != null;

            if (!stillBlocked && !hasPendingOrder)
            {
                // check current machine status in DB
                // if machine already restored itself (e.g. after [H] fix)
                // no command needed — resolution is just acknowledgement


                if (parkmeter != null &&
                    parkmeter.Status != MeterStatus.IN_SERVICE)
                {
                    // machine hasn't restored itself yet
                    // send command — machine will evaluate physically
                    await _mqttPublisher.PublishCommandAsync(
                        alert.ParkmeterId,
                        "SET_IN_SERVICE");

                    Console.WriteLine(
                        $"[COMMAND] SET_IN_SERVICE sent " +
                        $"→ {alert.ParkmeterId}");
                }
                else
                {
                    // machine already IN_SERVICE
                    // resolution is pure acknowledgement — no command needed
                    Console.WriteLine(
                        $"[ALERT] Resolved — machine already " +
                        $"IN_SERVICE, no command needed " +
                        $"(pure acknowledgement)");
                }
            }
            else
            {
                Console.WriteLine(
                    $"[ALERT] Resolved but meter stays blocked — " +
                    $"stillBlocked: {stillBlocked}, " +
                    $"hasPendingOrder: {hasPendingOrder}");
            }

            return true;
        }
    }
}