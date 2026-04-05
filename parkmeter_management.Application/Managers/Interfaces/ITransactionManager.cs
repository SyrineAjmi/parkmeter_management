using parkmeter_management.Application.Commands.Transactions;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Application.Managers.Interfaces
{
    public interface ITransactionManager
    {
        Task<Guid> RecordTransaction(CreateTransactionCommand command);
        Task<List<Transaction>> GetAllTransactions();
        Task<List<Transaction>> GetByParkmeter(Guid parkmeterId);
        Task<List<Transaction>> FilterByStatus(string status);
    }
}