using Microsoft.EntityFrameworkCore;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Infrastructure.Persistence.Repositories
{
    public class ParkmeterRepository : IParkmeterRepository
    {
        private readonly AppDbContext _context;

        public ParkmeterRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Parkmeter parkmeter)
        {
            await _context.Parkmeters.AddAsync(parkmeter);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Parkmeter>> GetAllAsync()
        {
            return await _context.Parkmeters
                .Include(p => p.Site)
                .ToListAsync();
        }

        public async Task<Parkmeter?> GetByIdAsync(Guid id)
        {
            return await _context.Parkmeters
                .Include(p => p.Site)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<IEnumerable<Parkmeter>> GetBySiteAsync(Guid siteId)
        {
            return await _context.Parkmeters
                .Where(p => p.SiteId == siteId)
                .ToListAsync();
        }

        public async Task UpdateAsync(Parkmeter parkmeter)
        {
            _context.Parkmeters.Update(parkmeter);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var parkmeter = await _context.Parkmeters.FindAsync(id);
            if (parkmeter != null)
            {
                _context.Parkmeters.Remove(parkmeter);
                await _context.SaveChangesAsync();
            }
        }

        public async Task <Parkmeter?>GetBySerialNumberAsync(string serialNumber)
        {
            return await _context.Parkmeters
                .FirstOrDefaultAsync(p => p.SerialNumber == serialNumber);
        }
    }
}