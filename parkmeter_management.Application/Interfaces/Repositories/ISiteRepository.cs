using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Application.Interfaces.Repositories
{
    public interface ISiteRepository
    {
        Task AddAsync(Site site);
        Task<IEnumerable<Site>> GetAllAsync();
        Task<Site?> GetByIdAsync(Guid id);
        Task UpdateAsync(Site site);
        Task DeleteAsync(Guid id);
    }
}