using MediatR;


namespace parkmeter_management.Application.Queries.Stats.Overview
{
    public record OverviewResult(
           int TotalParkmeters,
           int InService,
           int TemporarilyUnavailable,
           int OutOfService,
           int PendingActivation,
           int UnresolvedAlerts,
           int PendingInterventionOrders);

    public class GetOverviewQuery : IRequest<OverviewResult> { }
}
