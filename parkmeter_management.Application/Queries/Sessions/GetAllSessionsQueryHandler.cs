using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Application.Queries.Sessions
{
    public class GetAllSessionsQueryHandler
        : IRequestHandler<GetAllSessionsQuery, List<Session>>
    {
        private readonly ISessionRepository _sessionRepository;

        public GetAllSessionsQueryHandler(
            ISessionRepository sessionRepository)
        {
            _sessionRepository = sessionRepository;
        }

        public async Task<List<Session>> Handle(
            GetAllSessionsQuery request, CancellationToken ct)
        {
            var sessions = await _sessionRepository.GetAllAsync();
            return sessions.ToList();
        }
    }
}