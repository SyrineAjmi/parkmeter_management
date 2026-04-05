using MediatR;
using parkmeter_management.Application.Commands.ParkmeterConfiguration;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Application.Managers.Interfaces;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Application.Managers.Implementations
{
    public class ParkmeterConfigurationManager : IParkmeterConfigurationManager
    {
        private readonly IMediator _mediator;
        private readonly IParkmeterConfigurationRepository _configurationRepository;

        public ParkmeterConfigurationManager(
            IMediator mediator,
            IParkmeterConfigurationRepository configurationRepository)
        {
            _mediator = mediator;
            _configurationRepository = configurationRepository;
        }

        public async Task<bool> CreateConfig(
            Guid parkmeterId, decimal pricePerHour,
            int maxDuration)
        {
            return await _mediator.Send(new SendParkConfigCommand
            {
                ParkmeterId = parkmeterId,
                PricePerHour = pricePerHour,
                MaxDuration = maxDuration,
            });
        }

        public async Task<bool> UpdateConfig(
            Guid parkmeterId, decimal pricePerHour, int maxDuration)
        {
            var existing = await _configurationRepository
                .GetByParkMeterIdAsync(parkmeterId);
            if (existing == null) return false;

            return await _mediator.Send(new SendParkConfigCommand
            {
                ParkmeterId = parkmeterId,
                PricePerHour = pricePerHour,
                MaxDuration = maxDuration,
            });
        }

        public async Task<bool> DeleteConfig(Guid parkmeterId)
        {
            var existing = await _configurationRepository
                .GetByParkMeterIdAsync(parkmeterId);
            if (existing == null) return false;

            await _configurationRepository.DeleteAsync(parkmeterId);
            return true;
        }

        public async Task<ParkMeterConfig?> GetConfiguration(Guid parkmeterId)
        {
            return await _configurationRepository
                .GetByParkMeterIdAsync(parkmeterId);
        }

        public async Task<bool> ApplyToParkmeter(Guid parkmeterId)
        {
            var config = await _configurationRepository
                .GetByParkMeterIdAsync(parkmeterId);
            if (config == null) return false;

            return await _mediator.Send(new SendParkConfigCommand
            {
                ParkmeterId = parkmeterId,
                PricePerHour = config.PricePerHour,
                MaxDuration = config.MaxDuration,
            });
        }
    }
}