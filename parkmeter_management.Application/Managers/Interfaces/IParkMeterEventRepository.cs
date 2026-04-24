using parkmeter_management.Domain.Entities;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Application.Interfaces.Repositories
{
    public interface IParkMeterEventRepository
    {
        Task AddAsync(ParkMeterEvent parkMeterEvent);
        Task<IEnumerable<ParkMeterEvent>> GetAllAsync();
        Task<IEnumerable<ParkMeterEvent>> GetByParkmeterAsync(Guid parkmeterID);
        Task<IEnumerable<ParkMeterEvent>> GetByTypeAsync(ParkMeterEventType eventType);
        Task<IEnumerable<ParkMeterEvent>> GetByParkmeterAndTypeAsync(
            Guid parkmeterID, ParkMeterEventType eventType);
        Task<IEnumerable<ParkMeterEvent>> GetByDateRangeAsync(
            DateTime from, DateTime to);
    }
}