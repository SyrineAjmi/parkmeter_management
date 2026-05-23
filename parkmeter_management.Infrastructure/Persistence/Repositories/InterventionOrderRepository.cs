using Microsoft.EntityFrameworkCore;
using parkmeter_management.Application
    .Interfaces.Repositories;
using parkmeter_management.Domain.Entities;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Infrastructure
    .Persistence.Repositories
{
    public class InterventionOrderRepository
        : IInterventionOrderRepository
    {
        private readonly AppDbContext _context;

        public InterventionOrderRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(InterventionOrder order)
        {
            await _context.InterventionOrders.AddAsync(order);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(InterventionOrder order)
        {
            _context.InterventionOrders.Update(order);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<InterventionOrder>> GetAllAsync()
        {
            return await _context.InterventionOrders
                .Include(o => o.Operator)
                .Include(o => o.Parkmeter)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<InterventionOrder?> GetByIdAsync(Guid id)
            => await _context.InterventionOrders.FindAsync(id);

        public async Task<IEnumerable<InterventionOrder>> GetByParkmeterAsync(Guid parkmeterID)
        {
            return await _context.InterventionOrders
                .Where(o => o.ParkmeterId == parkmeterID)
                .Include(o => o.Operator)
                .Include(o => o.Parkmeter)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<InterventionOrder>> GetPendingAsync()
        {
            return await _context.InterventionOrders
                .Where(o => o.Status ==
                    InterventionOrderStatus.PENDING)
                .Include(o => o.Operator)
                .Include(o => o.Parkmeter)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<InterventionOrder>> GetByOperatorAsync(Guid operatorId)
        {
            return await _context.InterventionOrders
                .Include(o => o.Operator)
                .Include(o => o.Parkmeter)
                .Where(o => o.OperatorId == operatorId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<InterventionOrder?>
            GetPendingByParkmeterAndTypeAsync(
                Guid parkmeterID,
                InterventionType type)
        {
            return await _context.InterventionOrders
                .Include(o => o.Operator)
                .Include(o => o.Parkmeter)
                .FirstOrDefaultAsync(o =>
                    o.ParkmeterId == parkmeterID &&
                    o.Type == type && 
                    o.Status == InterventionOrderStatus.PENDING);
        }
    }
}