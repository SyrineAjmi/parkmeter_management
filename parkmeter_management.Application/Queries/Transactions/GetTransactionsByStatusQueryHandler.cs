using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Domain.Entities;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Application.Queries.Transactions
{
    public class GetTransactionsByStatusQueryHandler
        : IRequestHandler<GetTransactionsByStatusQuery, List<Transaction>>
    {
        private readonly ITransactionRepository _transactionRepository;

        public GetTransactionsByStatusQueryHandler(
            ITransactionRepository transactionRepository)
        {
            _transactionRepository = transactionRepository;
        }

        public async Task<List<Transaction>> Handle(
            GetTransactionsByStatusQuery request, CancellationToken ct)
        {
            if (!Enum.TryParse<TransactionStatus>(
                request.Status, true, out var status))
                return new List<Transaction>();

            var transactions = await _transactionRepository
                .GetByStatusAsync(status);
            return transactions.ToList();
        }
    }
}