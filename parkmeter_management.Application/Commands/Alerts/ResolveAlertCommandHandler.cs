using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Application.Interfaces.Services;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Application.Commands.Alerts
{
    public class ResolveAlertCommandHandler
        : IRequestHandler<ResolveAlertCommand, bool>
    {
        private readonly IAlertRepository _alertRepository;
        private readonly IParkmeterRepository _parkmeterRepository;
        private readonly IInterventionOrderRepository _interventionOrderRepository; // FIX #1
        private readonly IMqttPublisherService _mqttPublisher;
        private readonly INotificationService _notificationService;

        public ResolveAlertCommandHandler(
            IAlertRepository alertRepository,
            IParkmeterRepository parkmeterRepository,
            IInterventionOrderRepository interventionOrderRepository, // FIX #1
            IMqttPublisherService mqttPublisher,
            INotificationService notificationService)
        {
            _alertRepository = alertRepository;
            _parkmeterRepository = parkmeterRepository;
            _interventionOrderRepository = interventionOrderRepository; // FIX #1
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

            // notify front resolved
            await _notificationService.NotifyAlertResolved(
                request.AlertId,
                alert.ParkmeterId);

            // check if status should revert
            var remaining = await _alertRepository
                .GetByParkmeterAsync(alert.ParkmeterId);

            var stillBlocked = remaining.Any(a =>
                a.Status == AlertStatus.NOT_RESOLVED &&
                (a.Type == AlertType.CASH_BOX_FULL ||
                 a.Type == AlertType.PAPER_EMPTY ||
                 a.Type == AlertType.DOOR_OPEN ||
                 a.Type == AlertType.HARDWARE_FAILURE));

            // FIX #1 — also block restore if a maintenance intervention is still pending
            var hasPendingCashOrder = await _interventionOrderRepository
                .GetPendingByParkmeterAndTypeAsync(
                    alert.ParkmeterId,
                    InterventionType.CASH_COLLECTION);

            var hasPendingPaperOrder = await _interventionOrderRepository
                .GetPendingByParkmeterAndTypeAsync(
                    alert.ParkmeterId,
                    InterventionType.PAPER_REFILL);

            var hasPendingOrder = hasPendingCashOrder != null
                               || hasPendingPaperOrder != null;

            // to IN_SERVICE only if no blocking alerts AND no pending intervention
            if (!stillBlocked && !hasPendingOrder)
            {
                var parkmeter = await _parkmeterRepository
                    .GetByIdAsync(alert.ParkmeterId);

                if (parkmeter != null &&
                    (parkmeter.Status == MeterStatus.TEMPORARILY_UNAVAILABLE ||
                     parkmeter.Status == MeterStatus.MAINTENANCE)) // FIX #1 — also recover from MAINTENANCE
                {
                    parkmeter.Status = MeterStatus.IN_SERVICE;
                    await _parkmeterRepository.UpdateAsync(parkmeter);
                    await _mqttPublisher.PublishStatusAsync(
                        alert.ParkmeterId,
                        MeterStatus.IN_SERVICE.ToString());

                    // notify front status changed
                    await _notificationService.NotifyStatusChanged(
                        alert.ParkmeterId,
                        MeterStatus.IN_SERVICE.ToString());
                }
            }

            return true;
        }
    }
}