using MediatR;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Application.Queries.Transactions
{
    public class GetAllTransactionsQuery : IRequest<List<Transaction>>
    {
    }
}