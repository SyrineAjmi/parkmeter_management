using MediatR;
using parkmeter_management.Domain.Entities;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Application.Queries.Parkmeters
{
    public class GetParkmetersByStatusQuery : IRequest<List<Parkmeter>>
    {
        public MeterStatus Status { get; set; }
    }
}