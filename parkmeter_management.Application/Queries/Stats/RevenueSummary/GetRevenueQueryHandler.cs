using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Domain.Enums;


namespace parkmeter_management.Application.Queries.Stats.RevenueSummary
{
    public class GetRevenueQueryHandler
         : IRequestHandler<GetRevenueQuery, RevenueSummaryResult>
    {
        private readonly ITransactionRepository _transactionRepository;

        public GetRevenueQueryHandler(
            ITransactionRepository transactionRepository)
        {
            _transactionRepository = transactionRepository;
        }

        public async Task<RevenueSummaryResult> Handle(
            GetRevenueQuery query, CancellationToken ct)
        {
            var transactions = await _transactionRepository
                .GetByDateRangeAsync(query.From, query.To);

            var list = transactions.ToList();

            return new RevenueSummaryResult(
                TotalRevenue: list
                    .Where(t => t.Status == TransactionStatus.COMPLETED)
                    .Sum(t => t.Amount),
                TotalTransactions: list.Count,
                CompletedTransactions: list.Count(t =>
                    t.Status == TransactionStatus.COMPLETED),
                FailedTransactions: list.Count(t =>
                    t.Status == TransactionStatus.FAILED),
                CancelledTransactions: list.Count(t =>
                    t.Status == TransactionStatus.CANCELLED),
                ExpiredTransactions: list.Count(t =>
                    t.Status == TransactionStatus.EXPIRED),
                From: query.From,
                To: query.To);
        }
    }
}
