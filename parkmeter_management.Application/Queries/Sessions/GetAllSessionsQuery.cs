using MediatR;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Application.Queries.Sessions
{
    public class GetAllSessionsQuery : IRequest<List<Session>>
    {
    }
}