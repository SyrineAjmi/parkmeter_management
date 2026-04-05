using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Application.Queries.Transactions
{
    public class GetTransactionsByParkmeterQueryHandler
        : IRequestHandler<GetTransactionsByParkmeterQuery, List<Transaction>>
    {
        private readonly ITransactionRepository _transactionRepository;

        public GetTransactionsByParkmeterQueryHandler(
            ITransactionRepository transactionRepository)
        {
            _transactionRepository = transactionRepository;
        }

        public async Task<List<Transaction>> Handle(
            GetTransactionsByParkmeterQuery request, CancellationToken ct)
        {
            var transactions = await _transactionRepository
                .GetByParkMeterIdAsync(request.ParkmeterId);
            return transactions.ToList();
        }
    }
}