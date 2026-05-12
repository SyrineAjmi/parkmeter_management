using MediatR;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Application.Queries.Parkmeters
{
    public class GetParkmetersBySiteQuery : IRequest<List<Parkmeter>>
    {
        public Guid SiteId { get; set; }
    }
}