using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Application.Managers.Interfaces
{
    public interface IParkmeterConfigurationManager
    {
        Task<bool> CreateConfig(Guid parkmeterId, decimal pricePerHour,
            int maxDuration);
        Task<bool> UpdateConfig(Guid parkmeterId, decimal pricePerHour,
            int maxDuration);
        Task<bool> DeleteConfig(Guid parkmeterId);
        Task<ParkMeterConfig?> GetConfiguration(Guid parkmeterId);
        Task<bool> ApplyToParkmeter(Guid parkmeterId);

    }
}