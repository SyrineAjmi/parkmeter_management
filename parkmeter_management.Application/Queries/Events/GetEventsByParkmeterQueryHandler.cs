using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Application.Queries.Events
{
    public class GetEventsByParkmeterQueryHandler
        : IRequestHandler<GetEventsByParkmeterQuery, List<ParkMeterEvent>>
    {
        private readonly IParkMeterEventRepository _eventRepository;

        public GetEventsByParkmeterQueryHandler(
            IParkMeterEventRepository eventRepository)
        {
            _eventRepository = eventRepository;
        }

        public async Task<List<ParkMeterEvent>> Handle(
            GetEventsByParkmeterQuery request,
            CancellationToken ct)
        {
            var events = await _eventRepository
                .GetByParkmeterAsync(request.ParkmeterID);
            return events.ToList();
        }
    }
}