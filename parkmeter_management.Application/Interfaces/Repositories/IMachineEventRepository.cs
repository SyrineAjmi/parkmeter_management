using parkmeter_management.Domain.Entities;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Application.Interfaces.Repositories
{
    public interface IMachineEventRepository
    {
        Task AddAsync(MachineEvent machineEvent);
        Task<IEnumerable<MachineEvent>> GetByParkmeterAsync(Guid parkmeterID);
        Task<IEnumerable<MachineEvent>> GetByTypeAsync(MachineEventType eventType);
        Task<IEnumerable<MachineEvent>> GetByDateRangeAsync(DateTime from, DateTime to);
        Task<IEnumerable<MachineEvent>> GetAllAsync();

    }
}