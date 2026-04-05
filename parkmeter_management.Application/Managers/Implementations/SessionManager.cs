using MediatR;
using parkmeter_management.Application.Managers.Interfaces;
using parkmeter_management.Application.Queries.Sessions;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Application.Managers.Implementations
{
    public class SessionManager : ISessionManager
    {
        private readonly IMediator _mediator;

        public SessionManager(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task<List<Session>> GetAllSessions()
            => await _mediator.Send(new GetAllSessionsQuery());

        public async Task<List<Session>> GetByParkmeter(Guid parkmeterId)
            => await _mediator.Send(
                new GetSessionsByParkmeterQuery
                { ParkmeterId = parkmeterId });
    }
}