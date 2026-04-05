using parkmeter_management.Domain.Entities;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Application.Interfaces.Repositories
{
    public interface IAlertRepository
    {
        Task AddAsync(Alert alert);
        Task<IEnumerable<Alert>> GetAllAsync();
        Task<Alert?> GetByIdAsync(Guid id);
        Task<IEnumerable<Alert>> GetByPriorityAsync(Priority priority);

        Task UpdateAsync(Alert alert);

        Task<IEnumerable<Alert>> GetByParkmeterAsync(Guid parkmeterID);
        Task<IEnumerable<Alert>> GetUnresolvedAsync();

    }
}