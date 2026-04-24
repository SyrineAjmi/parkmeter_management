using MediatR;
using parkmeter_management.Application
    .Interfaces.Repositories;
using parkmeter_management.Application
    .Interfaces.Services;
using parkmeter_management.Domain.Entities;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.SimulatorApplication
    .Commands.HandleCashRemoved
{
    public class HandleCashRemovedCommandHandler
        : IRequestHandler<HandleCashRemovedCommand, bool>
    {
        private readonly IAlertRepository _alertRepository;
        private readonly IParkmeterRepository _parkmeterRepository;
        private readonly ISessionRepository _sessionRepository;
        private readonly IInterventionOrderRepository _interventionOrderRepository;
        private readonly IMqttPublisherService _mqttPublisher;
        private readonly INotificationService _notificationService;

        public HandleCashRemovedCommandHandler(
            IAlertRepository alertRepository,
            IParkmeterRepository parkmeterRepository,
            ISessionRepository sessionRepository,
            IInterventionOrderRepository interventionOrderRepository,
            IMqttPublisherService mqttPublisher,
            INotificationService notificationService)
        {
            _alertRepository = alertRepository;
            _parkmeterRepository = parkmeterRepository;
            _sessionRepository = sessionRepository;
            _interventionOrderRepository = interventionOrderRepository;
            _mqttPublisher = mqttPublisher;
            _notificationService = notificationService;
        }

        public async Task<bool> Handle(
            HandleCashRemovedCommand command,
            CancellationToken ct)
        {
            var parkmeterID = command.ParkmeterId;

            // close active session
            var session = await _sessionRepository
                .GetActiveSessionByParkmeterAsync(parkmeterID);

            if (session != null)
            {
                session.Status = SessionStatus.CLOSED;
                session.EndDateTime = DateTime.UtcNow;
                await _sessionRepository.UpdateAsync(session);

                await _notificationService.NotifySessionClosed(
                    parkmeterID,
                    session.TotalAmount,
                    session.TotalTransactions);

                Console.WriteLine(
                    $"Session closed | " +
                    $"Total: {session.TotalAmount} DT");
            }

            // open new session
            await _sessionRepository.AddAsync(new Session
            {
                Id = Guid.NewGuid(),
                ParkmeterId = parkmeterID,
                StartDateTime = DateTime.UtcNow,
                Status = SessionStatus.ACTIVE,
                TotalTransactions = 0,
                TotalAmount = 0
            });
            Console.WriteLine("New session started");

            // auto-resolve cash alerts
            var alerts = await _alertRepository
                .GetByParkmeterAsync(parkmeterID);
            foreach (var a in alerts.Where(a =>
                a.Status == AlertStatus.NOT_RESOLVED &&
                (a.Type == AlertType.CASH_BOX_FULL ||
                 a.Type == AlertType.CASH_BOX_WARN)))
            {
                a.Status = AlertStatus.RESOLVED;
                a.ResolvedAt = DateTime.UtcNow;
                await _alertRepository.UpdateAsync(a);
                Console.WriteLine($"Auto-resolved: {a.Type}");
            }

            // auto-complete intervention order
            var order = await _interventionOrderRepository
                .GetPendingByParkmeterAndTypeAsync(
                    parkmeterID,
                    InterventionType.CASH_COLLECTION);

            if (order != null)
            {
                order.Status = InterventionOrderStatus.COMPLETED;
                order.CompletedAt = DateTime.UtcNow;
                await _interventionOrderRepository
                    .UpdateAsync(order);

                await _notificationService
                    .NotifyInterventionOrderCompleted(
                        order.Id,
                        parkmeterID,
                        order.CompletedAt.Value);

                Console.WriteLine("✅ Intervention order completed");
            }

            // check stillBlocked
            var remaining = await _alertRepository
                .GetByParkmeterAsync(parkmeterID);
            var stillBlocked = remaining.Any(a =>
                a.Status == AlertStatus.NOT_RESOLVED &&
                (a.Type == AlertType.PAPER_EMPTY ||
                 a.Type == AlertType.HARDWARE_FAILURE ||
                 a.Type == AlertType.DOOR_OPEN));

            var otherOrder = await _interventionOrderRepository
                .GetPendingByParkmeterAndTypeAsync(
                    parkmeterID,
                    InterventionType.PAPER_REFILL);

            var parkmeter = await _parkmeterRepository
                .GetByIdAsync(parkmeterID);

            if (!stillBlocked &&
                parkmeter != null &&
                otherOrder == null &&
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
                Console.WriteLine("✅ IN_SERVICE");
            }

            // save event
            await _alertRepository.AddAsync(new Alert
            {
                Id = Guid.NewGuid(),
                ParkmeterId = parkmeterID,
                Type = AlertType.CASH_BOX_REMOVED,
                Priority = Priority.NORMAL,
                Description = "Cash box removed",
                Status = AlertStatus.RESOLVED,
                CreatedAt = DateTime.UtcNow,
                ResolvedAt = DateTime.UtcNow
            });

            return true;
        }
    }
}