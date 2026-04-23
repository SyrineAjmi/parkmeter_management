using parkmeter_management.Domain.Entities;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Application.Interfaces.Repositories
{
    public interface IInterventionOrderRepository
    {
        Task AddAsync(InterventionOrder order);
        Task UpdateAsync(InterventionOrder order);
        Task<IEnumerable<InterventionOrder>> GetAllAsync();
        Task<InterventionOrder?> GetByIdAsync(Guid id);
        Task<IEnumerable<InterventionOrder>> GetByParkmeterAsync(
            Guid parkmeterID);
        Task<IEnumerable<InterventionOrder>> GetPendingAsync();
        Task<IEnumerable<InterventionOrder>> GetByOperatorAsync(
            Guid operatorId);
        Task<InterventionOrder?> GetPendingByParkmeterAndTypeAsync(
            Guid parkmeterID,InterventionType type);
    }
}