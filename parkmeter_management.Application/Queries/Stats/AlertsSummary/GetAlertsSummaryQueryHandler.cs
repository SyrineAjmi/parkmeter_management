using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Domain.Enums;


namespace parkmeter_management.Application.Queries.Stats.AlertsSummary
{
    public class GetAlertsSummaryQueryHandler
        : IRequestHandler<GetAlertsSummaryQuery, AlertsSummaryResult>
    {
        private readonly IAlertRepository _alertRepository;

        public GetAlertsSummaryQueryHandler(
            IAlertRepository alertRepository)
        {
            _alertRepository = alertRepository;
        }

        public async Task<AlertsSummaryResult> Handle(
            GetAlertsSummaryQuery query, CancellationToken ct)
        {
            var unresolved = await _alertRepository
                .GetUnresolvedAsync();
            var list = unresolved.ToList();

            var byType = list
                .GroupBy(a => a.Type.ToString())
                .ToDictionary(g => g.Key, g => g.Count());

            return new AlertsSummaryResult(
                TotalUnresolved: list.Count,
                High: list.Count(a =>
                    a.Priority == Priority.HIGH),
                Normal: list.Count(a =>
                    a.Priority == Priority.NORMAL),
                Low: list.Count(a =>
                    a.Priority == Priority.LOW),
                ByType: byType);
        }
    }

}
