using MediatR;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Application.Queries.Transactions
{
    public class GetTransactionsBySessionQuery
        : IRequest<List<Transaction>>
    {
        public Guid SessionId { get; set; }
    }
}