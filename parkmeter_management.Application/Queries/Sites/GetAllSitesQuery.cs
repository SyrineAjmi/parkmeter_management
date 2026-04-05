using MediatR;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Application.Queries.Sites
{
    public class GetAllSitesQuery : IRequest<List<Site>>
    {
    }
}