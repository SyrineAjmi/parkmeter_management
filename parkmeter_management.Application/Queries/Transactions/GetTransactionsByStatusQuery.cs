using MediatR;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Application.Queries.Transactions
{
    public class GetTransactionsByStatusQuery : IRequest<List<Transaction>>
    {
        public string Status { get; set; } = string.Empty;
    }
}