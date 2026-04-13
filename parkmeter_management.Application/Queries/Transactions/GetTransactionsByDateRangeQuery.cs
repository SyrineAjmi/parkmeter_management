using MediatR;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Application.Queries.Transactions
{
    public class GetTransactionsByDateRangeQuery
        : IRequest<List<Transaction>>
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }
}