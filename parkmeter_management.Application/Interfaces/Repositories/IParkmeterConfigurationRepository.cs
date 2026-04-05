using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Application.Interfaces.Repositories
{
    public interface IParkmeterConfigurationRepository
    {
        Task SaveAsync(ParkMeterConfig configuration);
        Task<ParkMeterConfig?> GetByParkMeterIdAsync(Guid parkmeterId);
        Task DeleteAsync(Guid parkmeterId);
    }
}