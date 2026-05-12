using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Domain.Enums;


namespace parkmeter_management.Application.Queries.Stats.RevenueParkmeter
{
    public class GetRevenueByParkmeterQueryHandler
            : IRequestHandler<GetRevenueByParkmeterQuery,
                List<RevenueByParkmeterResult>>
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly IParkmeterRepository _parkmeterRepository;

        public GetRevenueByParkmeterQueryHandler(
            ITransactionRepository transactionRepository,
            IParkmeterRepository parkmeterRepository)
        {
            _transactionRepository = transactionRepository;
            _parkmeterRepository = parkmeterRepository;
        }

        public async Task<List<RevenueByParkmeterResult>> Handle(
            GetRevenueByParkmeterQuery query, CancellationToken ct)
        {
            var transactions = await _transactionRepository
                .GetByDateRangeAsync(query.From, query.To);

            var parkmeters = await _parkmeterRepository.GetAllAsync();
            var pmDict = parkmeters.ToDictionary(p => p.Id);

            return transactions
                .Where(t => t.Status == TransactionStatus.COMPLETED)
                .GroupBy(t => t.ParkmeterId)
                .Select(g => new RevenueByParkmeterResult(
                    ParkmeterID: g.Key,
                    SerialNumber: pmDict.ContainsKey(g.Key)
                        ? pmDict[g.Key].SerialNumber
                        : "Unknown",
                    TotalRevenue: g.Sum(t => t.Amount),
                    TotalTransactions: g.Count()))
                .OrderByDescending(r => r.TotalRevenue)
                .ToList();
        }
    }
}
