using Microsoft.EntityFrameworkCore;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Infrastructure.Persistence.Repositories
{
    public class ParkmeterConfigurationRepository : IParkmeterConfigurationRepository
    {
        private readonly AppDbContext _context;

        public ParkmeterConfigurationRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task SaveAsync(ParkMeterConfig configuration)
        {
            var existing = await _context.Configurations
                .FirstOrDefaultAsync(c => c.ParkmeterId == configuration.ParkmeterId);

            if (existing == null)
            {
                // First time config for this parkmeter
                await _context.Configurations.AddAsync(configuration);
            }
            else
            {
                // Update existing config
                existing.PricePerHour = configuration.PricePerHour;
                existing.MaxDuration = configuration.MaxDuration;
                existing.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<ParkMeterConfig?> GetByParkMeterIdAsync(Guid parkmeterId)
        {
            return await _context.Configurations
                .FirstOrDefaultAsync(c => c.ParkmeterId == parkmeterId);
        }
        public async Task DeleteAsync(Guid parkmeterId)
        {
            var config = await _context.Configurations
                .FirstOrDefaultAsync(c => c.ParkmeterId == parkmeterId);
            if (config != null)
            {
                _context.Configurations.Remove(config);
                await _context.SaveChangesAsync();
            }
        }
    }
}