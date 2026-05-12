using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Application.Interfaces.Services;
using parkmeter_management.Domain.Entities;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Application.Commands.InterventionOrders
{
    public class CreateInterventionOrderCommandHandler
        : IRequestHandler<CreateInterventionOrderCommand, Guid>
    {
        private readonly IInterventionOrderRepository _orderRepository;
        private readonly IParkmeterRepository _parkmeterRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMessagePublisherService _mqttPublisher;
        private readonly INotificationService _notificationService;

        public CreateInterventionOrderCommandHandler(
            IInterventionOrderRepository orderRepository,
            IParkmeterRepository parkmeterRepository,
            IUserRepository userRepository,
            IMessagePublisherService mqttPublisher,
            INotificationService notificationService)
        {
            _orderRepository = orderRepository;
            _parkmeterRepository = parkmeterRepository;
            _userRepository = userRepository;
            _mqttPublisher = mqttPublisher;
            _notificationService = notificationService;
        }

        public async Task<Guid> Handle(
            CreateInterventionOrderCommand command,
            CancellationToken ct)
        {
            // FIX: validate operator exists and has the correct role
            var @operator = await _userRepository.GetByIdAsync(command.OperatorId);
            if (@operator == null || @operator.Role != Role.OPERATEUR || !@operator.IsActive)
                throw new InvalidOperationException(
                    $"Operator {command.OperatorId} not found, inactive, or does not have the OPERATEUR role.");

            // validate parkmeter exists
            var parkmeter = await _parkmeterRepository.GetByIdAsync(command.ParkmeterId);
            if (parkmeter == null)
                throw new InvalidOperationException(
                    $"Parkmeter {command.ParkmeterId} not found.");

            // deduplicate: return existing pending order if already open
            var existing = await _orderRepository.GetPendingByParkmeterAndTypeAsync(
                command.ParkmeterId, command.Type);
            if (existing != null)
                return existing.Id;

            // create order
            var order = new InterventionOrder
            {
                Id = Guid.NewGuid(),
                ParkmeterId = command.ParkmeterId,
                OperatorId = command.OperatorId,
                Type = command.Type,
                Status = InterventionOrderStatus.PENDING,
                CreatedAt = DateTime.UtcNow
            };

            await _orderRepository.AddAsync(order);

            // move meter to MAINTENANCE
            parkmeter.Status = MeterStatus.MAINTENANCE;
            await _parkmeterRepository.UpdateAsync(parkmeter);
            await _mqttPublisher.PublishStatusAsync(
                command.ParkmeterId,
                MeterStatus.MAINTENANCE.ToString());

            // notify the assigned operator
            await _notificationService.NotifyInterventionOrderCreated(
                command.OperatorId,
                command.ParkmeterId,
                order.Id);

            return order.Id;
        }
    }
}