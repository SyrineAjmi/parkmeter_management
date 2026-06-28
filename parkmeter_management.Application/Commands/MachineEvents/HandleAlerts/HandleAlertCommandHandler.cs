using MediatR;
using parkmeter_management.Application.Commands.Alerts;
using parkmeter_management.Application.Helpers;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Application.Interfaces.Services;
using parkmeter_management.Domain.Entities;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Application.Commands.MachineEvents.HandleAlerts
{
    public class HandleAlertCommandHandler
        : IRequestHandler<HandleAlertCommand, bool>
    {
        private readonly IMediator _mediator;
        private readonly IInterventionOrderRepository _interventionOrderRepository;
        private readonly IUserRepository _userRepository;
        private readonly IParkmeterRepository _parkmeterRepository;
        private readonly INotificationService _notificationService;

        public HandleAlertCommandHandler(
            IMediator mediator,
            IInterventionOrderRepository interventionOrderRepository,
            IUserRepository userRepository,
            IParkmeterRepository parkmeterRepository,
            INotificationService notificationService)
        {
            _mediator = mediator;
            _interventionOrderRepository = interventionOrderRepository;
            _userRepository = userRepository;
            _parkmeterRepository = parkmeterRepository;
            _notificationService = notificationService;
        }

        public async Task<bool> Handle(
            HandleAlertCommand command,
            CancellationToken ct)
        {
            var combinedType = (AlertType)command.AlertType;

            foreach (AlertType type in Enum.GetValues<AlertType>())
            {
                if (type == AlertType.NONE) continue;
                if (!combinedType.HasFlag(type)) continue;

                var alertId = await _mediator.Send(
                    new CreateAlertCommand
                    {
                        ParkmeterId = command.ParkmeterId,
                        Type = type,
                        Description = $"{type} detected"
                    }, ct);

                Console.WriteLine(
                    $"Alert saved: {type} " +
                    $"(Priority: {AlertPriorityHelper.GetPriority(type)}) " +
                    $"→ {alertId}");

                // Auto-create intervention order for operational alerts
                if (type is AlertType.CASH_BOX_FULL or AlertType.PAPER_EMPTY)
                {
                    var parkmeter = await _parkmeterRepository
                        .GetByIdAsync(command.ParkmeterId);

                    if (parkmeter != null)
                        await CreateAutoInterventionOrder(parkmeter, type, ct);
                }
            }

            return true;
        }

        private async Task CreateAutoInterventionOrder(
            Parkmeter parkmeter,
            AlertType alertType,
            CancellationToken ct)
        {
            var interventionType = alertType == AlertType.CASH_BOX_FULL
                ? InterventionType.CASH_COLLECTION
                : InterventionType.PAPER_REFILL;

            // Avoid duplicate pending orders of the same type
            var existing = await _interventionOrderRepository
                .GetPendingByParkmeterAndTypeAsync(parkmeter.Id, interventionType);

            if (existing != null) return;

            // Find active operator assigned to the same site
            var operators = await _userRepository.GetAllAsync();
            var siteOperator = operators.FirstOrDefault(u =>
                u.Role == Role.OPERATEUR &&
                u.SiteId == parkmeter.SiteId &&
                u.IsActive);

            if (siteOperator == null)
            {
                Console.WriteLine(
                    $"[AUTO ORDER] No active operator for site {parkmeter.SiteId} " +
                    $"— skipping order creation");
                return;
            }

            var order = new InterventionOrder(
                parkmeter.Id,
                siteOperator.Id,
                interventionType);

            await _interventionOrderRepository.AddAsync(order);

            await _notificationService.NotifyInterventionOrderCreated(
                siteOperator.Id,
                parkmeter.SerialNumber,
                parkmeter.Id,
                order.Id,
                interventionType.ToString());

            Console.WriteLine(
                $"[AUTO ORDER] {interventionType} created for " +
                $"{parkmeter.SerialNumber} → {siteOperator.Name}");
        }
    }
}