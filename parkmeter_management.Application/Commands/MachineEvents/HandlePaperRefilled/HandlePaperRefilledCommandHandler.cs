using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Application.Interfaces.Services;
using parkmeter_management.Domain.Entities;
using parkmeter_management.Domain.Enums;
using System.Reflection.Metadata;

namespace parkmeter_management.Application.Commands.MachineEvents.HandlePaperRefilled
{
    public class HandlePaperRefilledCommandHandler
         : IRequestHandler<HandlePaperRefilledCommand, bool>
    {
        private readonly IAlertRepository _alertRepository;
        private readonly INotificationService _notificationService;
        private readonly IInterventionOrderRepository _interventionOrderRepository;
        private readonly IParkmeterRepository _parkmeterRepository;
        public HandlePaperRefilledCommandHandler(
            IAlertRepository alertRepository,
            INotificationService notificationService,
            IInterventionOrderRepository interventionOrderRepository,
            IParkmeterRepository parkmeterRepository)
        {
            _alertRepository = alertRepository;
            _notificationService = notificationService;
            _interventionOrderRepository = interventionOrderRepository;
            _parkmeterRepository = parkmeterRepository;
        }

        public async Task<bool> Handle(
            HandlePaperRefilledCommand command,
            CancellationToken ct)
        {
            // auto-resolve paper alerts
            var alerts = await _alertRepository
                .GetByParkmeterAsync(command.ParkmeterId);

            var parkmeter = await _parkmeterRepository.GetByIdAsync(command.ParkmeterId);

            foreach (var a in alerts.Where(a =>
                a.Status == AlertStatus.NOT_RESOLVED &&
                (a.Type == AlertType.PAPER_EMPTY ||
                 a.Type == AlertType.PAPER_LOW)))
            {
                a.Resolve();
                await _alertRepository.UpdateAsync(a);
                await _notificationService.NotifyAlertResolved( 
                    a.Id,
                    parkmeter?.SerialNumber ?? string.Empty,
                    a.ParkmeterId);

                Console.WriteLine($"Auto-resolved: {a.Type}");
            }

            // auto-complete intervention order
            var pendingOrder = await _interventionOrderRepository
                .GetPendingByParkmeterAndTypeAsync(
                    command.ParkmeterId,
                    InterventionType.PAPER_REFILL);



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
                Console.WriteLine("Paper refill order completed");
            }

            // no status push — machine will report IN_SERVICE itself
            Console.WriteLine(
                $"[PAPER_REFILLED] processed — " +
                $"machine will report its own status");

            return true;
        }
    }
}