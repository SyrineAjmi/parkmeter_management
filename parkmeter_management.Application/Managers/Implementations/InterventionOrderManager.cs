using MediatR;
using parkmeter_management.Application.Commands.InterventionOrders;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Application.Managers.Interfaces;
using parkmeter_management.Domain.Entities;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Application.Managers.Implementations
{    
    public class InterventionOrderManager : IInterventionOrderManager
    {
        private readonly IMediator _mediator;
        private readonly IInterventionOrderRepository _orderRepository;

        public InterventionOrderManager(
            IMediator mediator,
            IInterventionOrderRepository orderRepository)
        {
            _mediator = mediator;
            _orderRepository = orderRepository;
        }

        public async Task<Guid> CreateOrder(
            Guid parkmeterID, Guid operatorId, InterventionType type)
            => await _mediator.Send(new CreateInterventionOrderCommand
            {
                ParkmeterId = parkmeterID,
                OperatorId = operatorId,
                Type = type
            });

        public async Task<List<InterventionOrder>> GetAllOrders()
            => (await _orderRepository.GetAllAsync()).ToList();

        public async Task<List<InterventionOrder>> GetByParkmeter(Guid parkmeterID)
            => (await _orderRepository.GetByParkmeterAsync(parkmeterID)).ToList();

        public async Task<List<InterventionOrder>> GetPendingOrders()
            => (await _orderRepository.GetPendingAsync()).ToList();

        public async Task<List<InterventionOrder>> GetByOperator(Guid operatorId)
            => (await _orderRepository.GetByOperatorAsync(operatorId)).ToList();
    }
}