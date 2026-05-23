using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Application.Interfaces.Services;
using parkmeter_management.Domain.Entities;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Application.Commands.MachineEvents.HandleHardwareFixed
{
    public class HandleHardwareFixedCommandHandler
        : IRequestHandler<HandleHardwareFixedCommand, bool>
    {
        private readonly IInterventionOrderRepository _orderRepository;
        private readonly INotificationService _notificationService;
        private readonly IParkmeterRepository _parkmeterRepository;


        public HandleHardwareFixedCommandHandler(
            IInterventionOrderRepository orderRepository,
            INotificationService notificationService,
            IParkmeterRepository parkmeterRepository)
        {
            _orderRepository = orderRepository;
            _notificationService = notificationService;
            _parkmeterRepository = parkmeterRepository;
        }

        public async Task<bool> Handle(
            HandleHardwareFixedCommand command,
            CancellationToken ct)
        {
            // auto-complete HARDWARE_REPAIR order if exists
            var pendingOrder = await _orderRepository
                .GetPendingByParkmeterAndTypeAsync(
                    command.ParkmeterId,
                    InterventionType.HARDWARE_REPAIR);
            var parkmeter = await _parkmeterRepository
                .GetByIdAsync(command.ParkmeterId);

            if (pendingOrder != null)
            {
                pendingOrder.Status = InterventionOrderStatus.COMPLETED;
                pendingOrder.CompletedAt = DateTime.UtcNow;
                await _orderRepository.UpdateAsync(pendingOrder);

                if (parkmeter != null)
                {
                    await _notificationService
                    .NotifyInterventionOrderCompleted(
                        pendingOrder.Id,
                        parkmeter.SerialNumber,
                        command.ParkmeterId,
                        pendingOrder.CompletedAt.Value);
                }


                Console.WriteLine(
                    $"[HARDWARE_FIXED] Order completed: " +
                    $"{pendingOrder.Id}");
            }
            else
            {
                Console.WriteLine(
                    $"[HARDWARE_FIXED] No pending HARDWARE_REPAIR " +
                    $"order found — event recorded only");
            }

            return true;
        }
    }
}