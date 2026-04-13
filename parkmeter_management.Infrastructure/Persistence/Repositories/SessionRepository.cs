using Microsoft.EntityFrameworkCore;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Domain.Entities;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Infrastructure.Persistence.Repositories
{
    public class SessionRepository : ISessionRepository
    {
        private readonly AppDbContext _context;

        public SessionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Session session)
        {
            await _context.Sessions.AddAsync(session);
            await _context.SaveChangesAsync();
        }

        public async Task<Session?> GetActiveSessionByParkmeterAsync(
            Guid parkmeterID)
        {
            return await _context.Sessions 
                .FirstOrDefaultAsync(s =>
                    s.ParkmeterId == parkmeterID &&
                    s.Status == SessionStatus.ACTIVE);
        }

        public async Task UpdateAsync(Session session)
        {
            _context.Sessions.Update(session);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Session>> GetAllAsync()
        {
            return await _context.Sessions.ToListAsync();
        }

        public async Task<IEnumerable<Session>> GetByParkmeterAsync(
            Guid parkmeterID)
        {
            return await _context.Sessions
                .Where(s => s.ParkmeterId == parkmeterID)
                .OrderByDescending(s => s.StartDateTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<Session>> GetByDateRangeAsync(
            DateTime? from, DateTime? to)
        {
            var query = _context.Sessions.AsQueryable();

            if (from.HasValue)
                query = query.Where(s => s.StartDateTime >= from.Value);

            if (to.HasValue)
                query = query.Where(s => s.StartDateTime <= to.Value);

            return await query
                .OrderByDescending(s => s.StartDateTime)
                .ToListAsync();
        }
    }
}