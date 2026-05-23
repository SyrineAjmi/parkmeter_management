using Microsoft.EntityFrameworkCore;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Domain.Entities;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Infrastructure.Persistence.Repositories
{
    public class AlertRepository : IAlertRepository
    {
        private readonly AppDbContext _context;

        public AlertRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Alert alert)
        {
            await _context.Alerts.AddAsync(alert);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Alert>> GetAllAsync()
        {
            return await _context.Alerts
                .Include(a => a.Parkmeter)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<Alert?> GetByIdAsync(Guid id)
        {
            return await _context.Alerts
                .Include(a => a.Parkmeter)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<IEnumerable<Alert>> GetByPriorityAsync(Priority priority)
        {
            return await _context.Alerts
                .Include(a => a.Parkmeter)
                .Where(a => a.Priority == priority)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task UpdateAsync(Alert alert)
        {
            _context.Alerts.Update(alert);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Alert>> GetByParkmeterAsync(Guid parkmeterId)
        {
            return await _context.Alerts
                .Include(a => a.Parkmeter)
                .Where(a => a.ParkmeterId == parkmeterId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Alert>> GetUnresolvedAsync()
        {
            return await _context.Alerts
                .Include(a => a.Parkmeter)
                .Where(a => a.Status == AlertStatus.NOT_RESOLVED)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Alert>> GetUnresolvedByParkmeterAsync(Guid parkmeterId)
        {
            return await _context.Alerts
                .Include(a => a.Parkmeter)
                .Where(a => a.ParkmeterId == parkmeterId
                         && a.Status == AlertStatus.NOT_RESOLVED)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }
    }
}