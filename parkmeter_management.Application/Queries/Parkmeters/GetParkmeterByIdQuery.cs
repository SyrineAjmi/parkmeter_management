using MediatR;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Application.Queries.Parkmeters
{
    public class GetParkmeterByIdQuery : IRequest<Parkmeter?>
    {
        public Guid Id { get; set; }
    }
}