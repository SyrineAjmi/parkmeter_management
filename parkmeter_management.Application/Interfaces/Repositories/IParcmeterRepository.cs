using parkmeter_management.Domain.Entities;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Application.Interfaces.Repositories
{
    public interface IParkmeterRepository
    {
        Task AddAsync(Parkmeter parkmeter);
        Task<IEnumerable<Parkmeter>> GetAllAsync();
        Task<Parkmeter?> GetByIdAsync(Guid id);
        Task<IEnumerable<Parkmeter>> GetBySiteAsync(Guid siteId);
        Task UpdateAsync(Parkmeter parkmeter);
        Task DeleteAsync(Guid id);
        Task<Parkmeter?> GetBySerialNumberAsync(string serialNumber);

    }
}