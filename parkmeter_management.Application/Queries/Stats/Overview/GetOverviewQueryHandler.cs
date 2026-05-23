using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Domain.Enums;


namespace parkmeter_management.Application.Queries.Stats.Overview
{
    public class GetOverviewQueryHandler
        : IRequestHandler<GetOverviewQuery, OverviewResult>
    {
        private readonly IParkmeterRepository _parkmeterRepository;
        private readonly IAlertRepository _alertRepository;
        private readonly IInterventionOrderRepository _orderRepository;

        public GetOverviewQueryHandler(
            IParkmeterRepository parkmeterRepository,
            IAlertRepository alertRepository,
            IInterventionOrderRepository orderRepository)
        {
            _parkmeterRepository = parkmeterRepository;
            _alertRepository = alertRepository;
            _orderRepository = orderRepository;
        }

        public async Task<OverviewResult> Handle(
            GetOverviewQuery query, CancellationToken ct)
        {
            var parkmeters = await _parkmeterRepository.GetAllAsync();
            var unresolvedAlerts = await _alertRepository
                .GetUnresolvedAsync();
            var pendingOrders = await _orderRepository
                .GetPendingAsync();

            var pmList = parkmeters.ToList();

            return new OverviewResult(
                TotalParkmeters: pmList.Count,
                InService: pmList.Count(p =>
                    p.Status == MeterStatus.IN_SERVICE),
                TemporarilyUnavailable: pmList.Count(p =>
                    p.Status == MeterStatus.TEMPORARILY_UNAVAILABLE),
                OutOfService: pmList.Count(p =>
                    p.Status == MeterStatus.OUT_OF_SERVICE),
                PendingActivation: pmList.Count(p =>
                    p.Status == MeterStatus.PENDING_ACTIVATION),
                UnresolvedAlerts: unresolvedAlerts.Count(),
                PendingInterventionOrders: pendingOrders.Count());
        }
    }
}
