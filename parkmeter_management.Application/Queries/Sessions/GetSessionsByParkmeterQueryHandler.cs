using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Application.Queries.Sessions
{
    public class GetSessionsByParkmeterQueryHandler
        : IRequestHandler<GetSessionsByParkmeterQuery, List<Session>>
    {
        private readonly ISessionRepository _sessionRepository;

        public GetSessionsByParkmeterQueryHandler(
            ISessionRepository sessionRepository)
        {
            _sessionRepository = sessionRepository;
        }

        public async Task<List<Session>> Handle(
            GetSessionsByParkmeterQuery request, CancellationToken ct)
        {
            var sessions = await _sessionRepository
                .GetByParkmeterAsync(request.ParkmeterId);
            return sessions.ToList();
        }
    }
}