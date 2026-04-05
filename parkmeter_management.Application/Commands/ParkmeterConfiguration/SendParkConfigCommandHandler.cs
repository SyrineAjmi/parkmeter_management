using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Application.Interfaces.Services;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Application.Commands.ParkmeterConfiguration
{
    public class SendParkConfigCommandHandler : IRequestHandler<SendParkConfigCommand, bool>
    {
        private readonly IMqttPublisherService _mqttPublisher;
        private readonly IParkmeterConfigurationRepository _configurationRepository;

        public SendParkConfigCommandHandler(
            IMqttPublisherService mqttPublisher,
            IParkmeterConfigurationRepository configurationRepository)
        {
            _mqttPublisher = mqttPublisher;
            _configurationRepository = configurationRepository;

        }

        public async Task<bool> Handle(SendParkConfigCommand command, CancellationToken ct)
        {
            // save config to DB
            var config = new ParkMeterConfig
            {
                Id = Guid.NewGuid(),
                ParkmeterId = command.ParkmeterId,
                PricePerHour = command.PricePerHour,
                MaxDuration = command.MaxDuration,
                //  CreatedByUserId will be extracted from JWT token after authentication is implemented
                // Manual validation not needed here
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _configurationRepository.SaveAsync(config);

            //push config to parkmeter via MQTT
            await _mqttPublisher.PublishConfigAsync(
                command.ParkmeterId,
                command.PricePerHour,
                command.MaxDuration);

            return true;
        }
    }
}