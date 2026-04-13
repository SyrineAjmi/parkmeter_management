using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Application.Managers.Interfaces
{
    public interface ISessionManager
    {
        Task<List<Session>> GetAllSessions();
        Task<List<Session>> GetByParkmeter(Guid parkmeterId);
        Task<List<Session>> GetByDateRange(
            DateTime? from, DateTime? to);
    }
}