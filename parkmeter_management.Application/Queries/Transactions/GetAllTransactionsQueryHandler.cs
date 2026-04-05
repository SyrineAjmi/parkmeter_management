using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Application.Queries.Transactions
{
    public class GetAllTransactionsQueryHandler
        : IRequestHandler<GetAllTransactionsQuery, List<Transaction>>
    {
        private readonly ITransactionRepository _transactionRepository;

        public GetAllTransactionsQueryHandler(
            ITransactionRepository transactionRepository)
        {
            _transactionRepository = transactionRepository;
        }

        public async Task<List<Transaction>> Handle(
            GetAllTransactionsQuery request, CancellationToken ct)
        {
            var transactions = await _transactionRepository.GetAllAsync();
            return transactions.ToList();
        }
    }
}