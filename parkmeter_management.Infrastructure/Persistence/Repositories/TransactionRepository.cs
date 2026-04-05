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
            return await _context.Transactions.ToListAsync();
        }

        public async Task<Transaction?> GetByIdAsync(Guid id)
        {
            return await _context.Transactions.FindAsync(id);
        }

        public async Task<IEnumerable<Transaction>> GetByParkMeterIdAsync(
            Guid parkmeterId)
        {
            return await _context.Transactions
                .Where(t => t.ParkmeterId == parkmeterId)
                .ToListAsync();
        }
        public async Task<IEnumerable<Transaction>> GetByStatusAsync(
    TransactionStatus status)
        {
            return await _context.Transactions
                .Where(t => t.Status == status)
                .ToListAsync();
        }

    }
}