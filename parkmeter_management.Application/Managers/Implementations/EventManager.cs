using MediatR;
using parkmeter_management.Application.Queries.Events;
using parkmeter_management.Domain.Entities;

public class EventManager : IEventManager
{
    private readonly IMediator _mediator;

    public EventManager(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<List<ParkMeterEvent>> GetRecentByParkmeter(
        Guid parkmeterID, int count = 20)
        => (await _mediator.Send(
            new GetEventsByParkmeterQuery { ParkmeterID = parkmeterID }))
            .Take(count)
            .ToList();
}