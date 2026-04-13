using MediatR;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Application.Queries.Sessions
{
    public class GetSessionsByDateRangeQuery
        : IRequest<List<Session>>
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }
}