using Microsoft.EntityFrameworkCore;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Domain.Entities;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Infrastructure.Persistence.Repositories
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly AppDbContext _context;

        public TransactionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Transaction transaction)
        {
            await _context.Transactions.AddAsync(transaction);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Transaction>> GetAllAsync()
        {
            return await _context.Transactions
                 .Include(t => t.Parkmeter)
                 .OrderByDescending(t => t.CreatedAt)
                 .ToListAsync();
        }

        public async Task<Transaction?> GetByIdAsync(Guid id)
        {
            return await _context.Transactions.FindAsync(id);
        }

        public async Task<IEnumerable<Transaction>> GetByParkMeterIdAsync(
            Guid parkmeterId)
        {
            return await _context.Transactions
                .Include(t => t.Parkmeter)
                .Where(t => t.ParkmeterId == parkmeterId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }
        public async Task<IEnumerable<Transaction>> GetByStatusAsync(
            TransactionStatus status)
        {
            return await _context.Transactions
                .Include(t => t.Parkmeter)
                .Where(t => t.Status == status)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Transaction>> GetBySessionAsync(
            Guid sessionId)
        {
            return await _context.Transactions
                .Where(t => t.SessionId == sessionId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Transaction>> GetByDateRangeAsync(
            DateTime? from, DateTime? to)
        {
            var query = _context.Transactions.AsQueryable();

            if (from.HasValue)
                query = query.Where(t => t.CreatedAt >= from.Value);

            if (to.HasValue)
                query = query.Where(t => t.CreatedAt <= to.Value);

            return await query
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

    }
}