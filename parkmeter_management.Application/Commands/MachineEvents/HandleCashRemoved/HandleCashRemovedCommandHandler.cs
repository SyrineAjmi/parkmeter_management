using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Application.Interfaces.Services;
using parkmeter_management.Domain.Entities;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Application.Commands.MachineEvents.HandleCashRemoved
{
    public class HandleCashRemovedCommandHandler
        : IRequestHandler<HandleCashRemovedCommand, bool>
    {
        private readonly IAlertRepository _alertRepository;
        private readonly IParkmeterRepository _parkmeterRepository;
        private readonly ISessionRepository _sessionRepository;
        private readonly INotificationService _notificationService;
        private readonly IInterventionOrderRepository _interventionOrderRepository;

        public HandleCashRemovedCommandHandler(
            IAlertRepository alertRepository,
            IParkmeterRepository parkmeterRepository,
            ISessionRepository sessionRepository,
            INotificationService notificationService,
            IInterventionOrderRepository interventionOrderRepository)
        {
            _alertRepository = alertRepository;
            _parkmeterRepository = parkmeterRepository;
            _sessionRepository = sessionRepository;
            _notificationService = notificationService;
            _interventionOrderRepository = interventionOrderRepository;
        }

        public async Task<bool> Handle(
            HandleCashRemovedCommand command,
            CancellationToken ct)
        {
            // close active session
            var activeSession = await _sessionRepository
                .GetActiveSessionByParkmeterAsync(command.ParkmeterId);

            var parkmeter = await _parkmeterRepository
              .GetByIdAsync(command.ParkmeterId); 

            if (activeSession != null)
            {
                activeSession.Close();
                await _sessionRepository.UpdateAsync(activeSession);

                if (parkmeter != null)
                {
                    await _notificationService.NotifySessionClosed(
                    command.ParkmeterId,
                    parkmeter.SerialNumber,
                    activeSession.TotalAmount,
                    activeSession.TotalTransactions);
                }
                Console.WriteLine(
                    $"Session closed | Total: {activeSession.TotalAmount} DT");
            }

            // open new session
            await _sessionRepository.AddAsync(new Session(command.ParkmeterId));
            Console.WriteLine("New session started");

            // reset cash amount in DB

            if (parkmeter != null)
            {
                parkmeter.UpdateCashAmount(0);
                await _parkmeterRepository.UpdateAsync(parkmeter);
                Console.WriteLine($"[CASH_COLLECTED] CashAmount reset to 0");
            }

            // auto resolve cash alerts
            var alerts = await _alertRepository
                .GetByParkmeterAsync(command.ParkmeterId);
            foreach (var a in alerts.Where(a =>
                a.Status == AlertStatus.NOT_RESOLVED &&
                (a.Type == AlertType.CASH_BOX_FULL ||
                 a.Type == AlertType.CASH_BOX_WARN)))
            {
                a.Resolve();
                await _alertRepository.UpdateAsync(a);
                await _notificationService.NotifyAlertResolved(  
                    a.Id,
                    parkmeter?.SerialNumber ?? string.Empty,
                    a.ParkmeterId);
                Console.WriteLine($"Auto-resolved: {a.Type}");
            }

            // auto complete intervention order
            var pendingOrder = await _interventionOrderRepository
                .GetPendingByParkmeterAndTypeAsync(
                    command.ParkmeterId,
                    InterventionType.CASH_COLLECTION);

            if (pendingOrder != null)
            {
                pendingOrder.Complete();
                await _interventionOrderRepository.UpdateAsync(pendingOrder);

                if (parkmeter != null)
                {
                    await _notificationService.NotifyInterventionOrderCompleted(
                    pendingOrder.Id,
                    parkmeter.SerialNumber,
                    command.ParkmeterId,
                    pendingOrder.OperatorId,    
                    pendingOrder.CompletedAt.Value);
                }

                Console.WriteLine(
                    $"Intervention order completed: {pendingOrder.Id}");
            }

            // no status push — machine will report IN_SERVICE itself
            // via parkmeters/{id}/status topic after evaluating its conditions
            Console.WriteLine(
                $"[CASH_COLLECTED] processed — " +
                $"machine will report its own status");

            return true;
        }
    }
}


