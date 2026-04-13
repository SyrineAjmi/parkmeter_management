using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Application.Queries.Transactions
{
    public class GetTransactionsByDateRangeQueryHandler
        : IRequestHandler<GetTransactionsByDateRangeQuery,
            List<Transaction>>
    {
        private readonly ITransactionRepository _transactionRepository;

        public GetTransactionsByDateRangeQueryHandler(
            ITransactionRepository transactionRepository)
        {
            _transactionRepository = transactionRepository;
        }

        public async Task<List<Transaction>> Handle(
            GetTransactionsByDateRangeQuery request,
            CancellationToken ct)
        {
            var transactions = await _transactionRepository
                .GetByDateRangeAsync(request.From, request.To);
            return transactions.ToList();
        }
    }
}