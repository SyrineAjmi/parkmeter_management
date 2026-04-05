using MediatR;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Application.Queries.Sessions
{
    public class GetSessionsByParkmeterQuery : IRequest<List<Session>>
    {
        public Guid ParkmeterId { get; set; }
    }
}