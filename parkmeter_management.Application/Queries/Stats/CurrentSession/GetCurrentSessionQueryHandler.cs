using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace parkmeter_management.Application.Queries.Stats.CurrentSession
{
    public class GetCurrentSessionQueryHandler
            : IRequestHandler<GetCurrentSessionQuery,
                CurrentSessionResult?>
    {
        private readonly ISessionRepository _sessionRepository;
        private readonly IParkmeterRepository _parkmeterRepository;

        public GetCurrentSessionQueryHandler(
            ISessionRepository sessionRepository,
            IParkmeterRepository parkmeterRepository)
        {
            _sessionRepository = sessionRepository;
            _parkmeterRepository = parkmeterRepository;
        }

        public async Task<CurrentSessionResult?> Handle(
            GetCurrentSessionQuery query, CancellationToken ct)
        {
            var session = await _sessionRepository
                .GetActiveSessionByParkmeterAsync(query.ParkmeterID);

            if (session == null) return null;

            var parkmeter = await _parkmeterRepository
                .GetByIdAsync(query.ParkmeterID);

            return new CurrentSessionResult(
                ParkmeterID: query.ParkmeterID,
                SerialNumber: parkmeter?.SerialNumber ?? "Unknown",
                SessionStart: session.StartDateTime,
                TotalTransactions: session.TotalTransactions,
                TotalAmount: session.TotalAmount,
                Status: session.Status.ToString());
        }
    }
}
