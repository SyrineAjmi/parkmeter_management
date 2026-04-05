using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Application.Interfaces.Repositories
{
    public interface ISessionRepository
    {
        Task AddAsync(Session session);
        Task<Session?> GetActiveSessionByParkmeterAsync(Guid parkmeterId);
        Task UpdateAsync(Session session);
        Task<IEnumerable<Session>> GetAllAsync();
        Task<IEnumerable<Session>> GetByParkmeterAsync(Guid parkmeterId);
    }
}