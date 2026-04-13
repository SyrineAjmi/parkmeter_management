using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Application.Queries.Transactions
{
    public class GetTransactionsBySessionQueryHandler
        : IRequestHandler<GetTransactionsBySessionQuery,
            List<Transaction>>
    {
        private readonly ITransactionRepository _transactionRepository;

        public GetTransactionsBySessionQueryHandler(
            ITransactionRepository transactionRepository)
        {
            _transactionRepository = transactionRepository;
        }

        public async Task<List<Transaction>> Handle(
            GetTransactionsBySessionQuery request,
            CancellationToken ct)
        {
            var transactions = await _transactionRepository
                .GetBySessionAsync(request.SessionId);
            return transactions.ToList();
        }
    }
}