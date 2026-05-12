using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Application.Interfaces.Services;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Application.Commands.ParkmeterConfiguration
{
    public class SendParkConfigCommandHandler
        : IRequestHandler<SendParkConfigCommand, bool>
    {
        private readonly IMessagePublisherService _mqttPublisher;
        private readonly IParkmeterConfigurationRepository _configurationRepository;

        public SendParkConfigCommandHandler(
            IMessagePublisherService mqttPublisher,
            IParkmeterConfigurationRepository configurationRepository)
        {
            _mqttPublisher = mqttPublisher;
            _configurationRepository = configurationRepository;
        }

        public async Task<bool> Handle(
            SendParkConfigCommand command, CancellationToken ct)
        {
            // save config to DB
            var config = new ParkMeterConfig
            {
                Id = Guid.NewGuid(),
                ParkmeterId = command.ParkmeterId,
                MaxDuration = command.MaxDuration,
                StrategyType = command.StrategyType,
                PricePerHour = command.PricePerHour,
                PeakRate = command.PeakRate,
                OffPeakRate = command.OffPeakRate,
                WeekdayRate = command.WeekdayRate,
                WeekendRate = command.WeekendRate,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _configurationRepository.SaveAsync(config);

            // push full strategy to parkmeter via MQTT
            await _mqttPublisher.PublishConfigAsync(
                command.ParkmeterId, config);

            return true;
        }
    }
}