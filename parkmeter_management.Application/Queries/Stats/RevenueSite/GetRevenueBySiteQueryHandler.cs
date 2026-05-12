using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Domain.Enums;


namespace parkmeter_management.Application.Queries.Stats.RevenueSite
{
    public class GetRevenueBySiteQueryHandler
          : IRequestHandler<GetRevenueBySiteQuery,
              List<RevenueBySiteResult>>
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly IParkmeterRepository _parkmeterRepository;
        private readonly ISiteRepository _siteRepository;

        public GetRevenueBySiteQueryHandler(
            ITransactionRepository transactionRepository,
            IParkmeterRepository parkmeterRepository,
            ISiteRepository siteRepository)
        {
            _transactionRepository = transactionRepository;
            _parkmeterRepository = parkmeterRepository;
            _siteRepository = siteRepository;
        }

        public async Task<List<RevenueBySiteResult>> Handle(
            GetRevenueBySiteQuery query, CancellationToken ct)
        {
            var transactions = await _transactionRepository
                .GetByDateRangeAsync(query.From, query.To);

            var parkmeters = await _parkmeterRepository.GetAllAsync();
            var sites = await _siteRepository.GetAllAsync();

            var pmDict = parkmeters.ToDictionary(p => p.Id);
            var siteDict = sites.ToDictionary(s => s.Id);

            return transactions
                .Where(t => t.Status == TransactionStatus.COMPLETED)
                .GroupBy(t =>
                    pmDict.ContainsKey(t.ParkmeterId)
                        ? pmDict[t.ParkmeterId].SiteId
                        : Guid.Empty)
                .Where(g => g.Key != Guid.Empty)
                .Select(g => new RevenueBySiteResult(
                    SiteId: g.Key,
                    SiteName: siteDict.ContainsKey(g.Key)
                        ? siteDict[g.Key].Name
                        : "Unknown",
                    TotalRevenue: g.Sum(t => t.Amount),
                    TotalTransactions: g.Count()))
                .OrderByDescending(r => r.TotalRevenue)
                .ToList();
        }
    }
}
