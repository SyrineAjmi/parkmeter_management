using parkmeter_management.Domain.Entities;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Application.Managers.Interfaces
{
    public interface IParkmeterConfigurationManager
    {
        Task<bool> CreateConfig(
            Guid parkmeterId,
            int maxDuration,
            PricingStrategyType strategyType,
            decimal? pricePerHour,
            decimal? peakRate,
            decimal? offPeakRate,
            decimal? weekdayRate,
            decimal? weekendRate);

        Task<bool> UpdateConfig(
            Guid parkmeterId,
            int maxDuration,
            PricingStrategyType strategyType,
            decimal? pricePerHour,
            decimal? peakRate,
            decimal? offPeakRate,
            decimal? weekdayRate,
            decimal? weekendRate);

        Task<bool> DeleteConfig(Guid parkmeterId);
        Task<ParkMeterConfig?> GetConfiguration(Guid parkmeterId);
        Task<bool> ApplyToParkmeter(Guid parkmeterId);
    }
}