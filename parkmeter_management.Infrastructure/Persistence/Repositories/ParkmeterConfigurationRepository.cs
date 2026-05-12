using Microsoft.EntityFrameworkCore;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Infrastructure.Persistence.Repositories
{
    public class ParkmeterConfigurationRepository
        : IParkmeterConfigurationRepository
    {
        private readonly AppDbContext _context;

        public ParkmeterConfigurationRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task SaveAsync(ParkMeterConfig configuration)
        {
            var existing = await _context.Configurations
                .FirstOrDefaultAsync(c =>
                    c.ParkmeterId == configuration.ParkmeterId);

            if (existing == null)
            {
                await _context.Configurations.AddAsync(configuration);
            }
            else
            {
                // base
                existing.MaxDuration = configuration.MaxDuration;
                existing.UpdatedAt = DateTime.UtcNow;

                // strategy
                existing.StrategyType = configuration.StrategyType;
                existing.PricePerHour = configuration.PricePerHour;
                existing.PeakRate = configuration.PeakRate;
                existing.OffPeakRate = configuration.OffPeakRate;
                existing.WeekdayRate = configuration.WeekdayRate;
                existing.WeekendRate = configuration.WeekendRate;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<ParkMeterConfig?> GetByParkMeterIdAsync(
            Guid parkmeterId)
        {
            return await _context.Configurations
                .FirstOrDefaultAsync(c =>
                    c.ParkmeterId == parkmeterId);
        }

        public async Task DeleteAsync(Guid parkmeterId)
        {
            var config = await _context.Configurations
                .FirstOrDefaultAsync(c =>
                    c.ParkmeterId == parkmeterId);

            if (config != null)
            {
                _context.Configurations.Remove(config);
                await _context.SaveChangesAsync();
            }
        }
    }
}