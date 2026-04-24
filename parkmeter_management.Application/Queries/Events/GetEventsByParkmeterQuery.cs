using MediatR;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Application.Queries.Events
{
    public class GetEventsByParkmeterQuery : IRequest<List<ParkMeterEvent>>
    {
        public Guid ParkmeterID { get; set; }
    }
}