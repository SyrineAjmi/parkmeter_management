using parkmeter_management.Api.s;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Api.Mappers
{
    public static class SessionMapper
    {
        public static SessionDto ToDto(this Session session)
            => new SessionDto
            {
                Id = session.Id,
                ParkmeterId = session.ParkmeterId,
                ParkmeterSerial = session.Parkmeter?.SerialNumber ?? string.Empty,
                StartDateTime = session.StartDateTime,
                EndDateTime = session.EndDateTime,
                TotalTransactions = session.TotalTransactions,
                TotalAmount = session.TotalAmount,
                Status = session.Status.ToString()
            };

        public static IEnumerable<SessionDto> ToDtos(
            this IEnumerable<Session> sessions)
            => sessions.Select(s => s.ToDto());
    }
}