using MediatR;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Application.Queries.Transactions
{
    public class GetTransactionsByParkmeterQuery : IRequest<List<Transaction>>
    {
        public Guid ParkmeterId { get; set; }
    }
}