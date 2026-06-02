using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Application.Interfaces.Services;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Application.Commands.MachineEvents.HandleReconnected
{
    public class HandleReconnectedCommandHandler
        : IRequestHandler<HandleReconnectedCommand, bool>
    {
        private readonly IAlertRepository _alertRepository;
        private readonly IMessagePublisherService _mqttPublisher;
        private readonly INotificationService _notificationService;
        private readonly IParkmeterRepository _parkmeterRepository;
        private readonly IInterventionOrderRepository _orderRepository;


        public HandleReconnectedCommandHandler(
            IAlertRepository alertRepository,
            IMessagePublisherService mqttPublisher,
            INotificationService notificationService,
            IParkmeterRepository parkmeterRepository,
            IInterventionOrderRepository orderRepository) 

        {
            _alertRepository = alertRepository;
            _mqttPublisher = mqttPublisher;
            _notificationService = notificationService;
            _parkmeterRepository = parkmeterRepository;
            _orderRepository = orderRepository; 

        }

        public async Task<bool> Handle(
            HandleReconnectedCommand command,
            CancellationToken ct)
        {
            // auto-resolve CONNECTION_LOST alert
            var alerts = await _alertRepository
                .GetByParkmeterAsync(command.ParkmeterId);

            foreach (var a in alerts.Where(a =>
                a.Status == AlertStatus.NOT_RESOLVED &&
                a.Type == AlertType.CONNECTION_LOST))
            {
                a.Resolve();
                await _alertRepository.UpdateAsync(a);
                Console.WriteLine(
                    "Auto-resolved on reconnect: CONNECTION_LOST");
            }

            // check remaining blocking alerts
            var remaining = await _alertRepository
                .GetByParkmeterAsync(command.ParkmeterId);

            var stillBlocked = remaining.Any(a =>
                a.Status == AlertStatus.NOT_RESOLVED &&
                (a.Type == AlertType.CASH_BOX_FULL ||
                 a.Type == AlertType.PAPER_EMPTY ||
                 a.Type == AlertType.DOOR_OPEN ||
                 a.Type == AlertType.HARDWARE_FAILURE));

            if (!stillBlocked)
            {
                var parkmeter = await _parkmeterRepository
                    .GetByIdAsync(command.ParkmeterId);

                // check pending hardware repair order
                var pendingHardwareOrder = await _orderRepository
                    .GetPendingByParkmeterAndTypeAsync(
                        command.ParkmeterId,
                        InterventionType.HARDWARE_REPAIR);

                if (parkmeter != null &&
                    parkmeter.Status != MeterStatus.PENDING_ACTIVATION &&
                    parkmeter.Status != MeterStatus.IN_SERVICE &&
                    pendingHardwareOrder == null) 
                {
                    await _mqttPublisher.PublishCommandAsync(
                        command.ParkmeterId,
                        "SET_IN_SERVICE");

                    Console.WriteLine(
                        $"[COMMAND] SET_IN_SERVICE sent after reconnect " +
                        $"→ {command.ParkmeterId}");
                }
            }

            return true;
        }
    }
}