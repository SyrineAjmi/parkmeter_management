using Microsoft.EntityFrameworkCore;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Domain.Entities;
using parkmeter_management.Domain.Enums;
using parkmeter_management.Infrastructure.Persistence;

namespace parkmeter_management.Infrastructure.Persistence.Repositories
{
    public class MachineEventRepository : IMachineEventRepository
    {
        private readonly AppDbContext _context;

        public MachineEventRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(MachineEvent machineEvent)
        {
            await _context.MachineEvents.AddAsync(machineEvent);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<MachineEvent>> GetByParkmeterAsync(Guid parkmeterID)
            => await _context.MachineEvents
                .Where(e => e.ParkmeterId == parkmeterID)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

        public async Task<IEnumerable<MachineEvent>> GetByTypeAsync(MachineEventType eventType)
            => await _context.MachineEvents
                .Where(e => (e.EventType & eventType) == eventType)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

        public async Task<IEnumerable<MachineEvent>> GetByDateRangeAsync(DateTime from, DateTime to)
            => await _context.MachineEvents
                .Where(e => e.CreatedAt >= from && e.CreatedAt <= to)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

        public async Task<IEnumerable<MachineEvent>> GetAllAsync()
        => await _context.MachineEvents
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }
}