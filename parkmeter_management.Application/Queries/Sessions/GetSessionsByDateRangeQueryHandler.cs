using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Application.Queries.Sessions
{
    public class GetSessionsByDateRangeQueryHandler
        : IRequestHandler<GetSessionsByDateRangeQuery, List<Session>>
    {
        private readonly ISessionRepository _sessionRepository;

        public GetSessionsByDateRangeQueryHandler(
            ISessionRepository sessionRepository)
        {
            _sessionRepository = sessionRepository;
        }

        public async Task<List<Session>> Handle(
            GetSessionsByDateRangeQuery request,
            CancellationToken ct)
        {
            var sessions = await _sessionRepository
                .GetByDateRangeAsync(request.From, request.To);
            return sessions.ToList();
        }
    }
}