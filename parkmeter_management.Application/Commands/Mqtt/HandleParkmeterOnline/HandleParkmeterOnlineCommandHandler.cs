using MediatR;
using parkmeter_management.Application
    .Interfaces.Repositories;
using parkmeter_management.Application
    .Interfaces.Services;
using parkmeter_management.Domain.Enums;
using System.Text.Json;

namespace parkmeter_management.Application.Commands.Mqtt.HandleParkmeterOnline
{
    public class HandleParkmeterOnlineCommandHandler
        : IRequestHandler<HandleParkmeterOnlineCommand, bool>
    {
        private readonly IParkmeterRepository
            _parkmeterRepository;
        private readonly IParkmeterConfigurationRepository
            _configurationRepository;
        private readonly IMqttPublisherService
            _mqttPublisher;
        private readonly INotificationService
            _notificationService;

        public HandleParkmeterOnlineCommandHandler(
            IParkmeterRepository parkmeterRepository,
            IParkmeterConfigurationRepository configurationRepository,
            IMqttPublisherService mqttPublisher,
            INotificationService notificationService)
        {
            _parkmeterRepository = parkmeterRepository;
            _configurationRepository = configurationRepository;
            _mqttPublisher = mqttPublisher;
            _notificationService = notificationService;
        }

        public async Task<bool> Handle(
            HandleParkmeterOnlineCommand command,
            CancellationToken ct)
        {
            var raw = JsonSerializer
                .Deserialize<JsonElement>(command.Payload);
            var parkmeterID = Guid.Parse(
                raw.GetProperty("ParkmeterId").GetString()!);

            var parkmeter = await _parkmeterRepository
                .GetByIdAsync(parkmeterID);
            if (parkmeter == null) return false;

            var config = await _configurationRepository
                .GetByParkMeterIdAsync(parkmeterID);

            Console.WriteLine(
                $"[ONLINE] {parkmeter.SerialNumber} | " +
                $"Status: {parkmeter.Status} | " +
                $"Config: {(config != null ? "yes" : "no")}");

            // push config always if exists
            if (config != null)
                await _mqttPublisher.PublishConfigAsync(
                    parkmeterID,
                    config.PricePerHour,
                    config.MaxDuration);

            // auto-activate ONLY if PENDING_ACTIVATION & config
            if (config != null &&
                parkmeter.Status == MeterStatus.PENDING_ACTIVATION)
            {
                parkmeter.Status = MeterStatus.IN_SERVICE;
                await _parkmeterRepository.UpdateAsync(parkmeter);

                await _mqttPublisher.PublishStatusAsync(
                    parkmeterID,
                    MeterStatus.IN_SERVICE.ToString());

                await _notificationService.NotifyStatusChanged(
                    parkmeterID,
                    MeterStatus.IN_SERVICE.ToString());

                Console.WriteLine(
                    $"{parkmeter.SerialNumber} " +
                    $"auto-activated : IN_SERVICE");
            }
            else
            {
                // just push current status 
                await _mqttPublisher.PublishStatusAsync(
                    parkmeterID,
                    parkmeter.Status.ToString());

                Console.WriteLine(
                    $"[ONLINE] {parkmeter.SerialNumber} " +
                    $"→ status: {parkmeter.Status}");
            }

            return true;
        }
    }
}