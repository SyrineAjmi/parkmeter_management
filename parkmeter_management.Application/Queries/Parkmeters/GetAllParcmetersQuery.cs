using MediatR;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Application.Queries.Parkmeters
{
    public class GetAllParkmetersQuery : IRequest<List<Parkmeter>>
    {
    }
}