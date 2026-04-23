using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Application.Interfaces.Services;
using parkmeter_management.Application.Managers.Interfaces;
using parkmeter_management.Domain.Entities;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Application.Managers.Implementations
{
    public class InterventionOrderManager : IInterventionOrderManager
    {
        private readonly IInterventionOrderRepository _interventionOrderRepository;
        private readonly INotificationService _notificationService;
        private readonly IParkmeterRepository _parkmeterRepository;
        private readonly IMqttPublisherService _mqttPublisher;


        public InterventionOrderManager(
            IInterventionOrderRepository interventionOrderRepository, INotificationService notificationService,
            IParkmeterRepository parkmeterRepository ,
            IMqttPublisherService mqttPublisher)
        {
            _interventionOrderRepository = interventionOrderRepository;
            _parkmeterRepository = parkmeterRepository;
            _notificationService = notificationService;
            _mqttPublisher = mqttPublisher;
        }

        public async Task<Guid> CreateOrder(Guid parkmeterID, Guid operatorId, InterventionType type)
        {
            var existing = await _interventionOrderRepository
                .GetPendingByParkmeterAndTypeAsync(
                    parkmeterID,
                    type);
            if (existing != null) return existing.Id;

            var order = new InterventionOrder
            {
                Id = Guid.NewGuid(),
                ParkmeterId = parkmeterID,
                OperatorId = operatorId,
                Type = type,    
                Status = InterventionOrderStatus.PENDING,
                CreatedAt = DateTime.UtcNow
            };

            await _interventionOrderRepository.AddAsync(order);

            await _mqttPublisher.PublishStatusAsync(
                  parkmeterID,
                  MeterStatus.MAINTENANCE.ToString());

            var parkmeter = await _parkmeterRepository
                .GetByIdAsync(parkmeterID);
            if (parkmeter != null)
            {
                parkmeter.Status = MeterStatus.MAINTENANCE;
                await _parkmeterRepository.UpdateAsync(parkmeter);
            }

            // notify operator 
            await _notificationService
                .NotifyInterventionOrderCreated(
                    operatorId, 
                    parkmeterID,
                    order.Id);

            return order.Id;
        }

        public async Task<List<InterventionOrder>> GetAllOrders()
        {
            var orders = await _interventionOrderRepository
                .GetAllAsync();
            return orders.ToList();
        }

        public async Task<List<InterventionOrder>> GetByParkmeter(
            Guid parkmeterID)
        {
            var orders = await _interventionOrderRepository
                .GetByParkmeterAsync(parkmeterID);
            return orders.ToList();
        }

        public async Task<List<InterventionOrder>> GetPendingOrders()
        {
            var orders = await _interventionOrderRepository
                .GetPendingAsync();
            return orders.ToList();
        }

        public async Task<List<InterventionOrder>> GetByOperator(
             Guid operatorId) 
        {
            var orders = await _interventionOrderRepository
                .GetByOperatorAsync(operatorId);
            return orders.ToList();
        }
    }
}