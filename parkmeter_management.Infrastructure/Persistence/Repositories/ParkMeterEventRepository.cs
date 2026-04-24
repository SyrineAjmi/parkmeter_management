using Microsoft.EntityFrameworkCore;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Domain.Entities;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Infrastructure.Persistence.Repositories
{
    public class ParkMeterEventRepository : IParkMeterEventRepository
    {
        private readonly AppDbContext _context;

        public ParkMeterEventRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(ParkMeterEvent parkMeterEvent)
        {
            await _context.ParkMeterEvents.AddAsync(parkMeterEvent);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<ParkMeterEvent>> GetAllAsync()
        {
            return await _context.ParkMeterEvents
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<ParkMeterEvent>> GetByParkmeterAsync(
            Guid parkmeterID)
        {
            return await _context.ParkMeterEvents
                .Where(e => e.ParkmeterId == parkmeterID)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<ParkMeterEvent>> GetByTypeAsync(
            ParkMeterEventType eventType)
        {
            return await _context.ParkMeterEvents
                .Where(e => e.EventType == eventType)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<ParkMeterEvent>> GetByParkmeterAndTypeAsync(
            Guid parkmeterID, ParkMeterEventType eventType)
        {
            return await _context.ParkMeterEvents
                .Where(e => e.ParkmeterId == parkmeterID
                         && e.EventType == eventType)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<ParkMeterEvent>> GetByDateRangeAsync(
            DateTime from, DateTime to)
        {
            return await _context.ParkMeterEvents
                .Where(e => e.CreatedAt >= from && e.CreatedAt <= to)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }
    }
}