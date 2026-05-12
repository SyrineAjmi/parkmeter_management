using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Application.Interfaces.Services;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Application.Commands.MachineEvents.HandleParkmeterOnline
{
    public class HandleParkmeterOnlineCommandHandler
        : IRequestHandler<HandleParkmeterOnlineCommand, bool>
    {
        private readonly IParkmeterRepository
            _parkmeterRepository;
        private readonly IParkmeterConfigurationRepository
            _configurationRepository;
        private readonly IMessagePublisherService
            _mqttPublisher;
        private readonly INotificationService
            _notificationService;

        public HandleParkmeterOnlineCommandHandler(
            IParkmeterRepository parkmeterRepository,
            IParkmeterConfigurationRepository configurationRepository,
            IMessagePublisherService mqttPublisher,
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
            var parkmeter = await _parkmeterRepository
                .GetByIdAsync(command.ParkmeterId);
            if (parkmeter == null) return false;

            var config = await _configurationRepository
                .GetByParkMeterIdAsync(command.ParkmeterId);

            Console.WriteLine(
                $"[ONLINE] {parkmeter.SerialNumber} | " +
                $"Status: {parkmeter.Status} | " +
                $"Config: {(config != null ? "yes" : "no")}");

            // push config always if exists
            if (config != null)
                await _mqttPublisher.PublishConfigAsync(
                    command.ParkmeterId,
                    config);

            // auto-activate ONLY if PENDING_ACTIVATION & config
            if (config != null &&
                parkmeter.Status == MeterStatus.PENDING_ACTIVATION)
            {
                parkmeter.Status = MeterStatus.IN_SERVICE;
                await _parkmeterRepository.UpdateAsync(parkmeter);

                await _mqttPublisher.PublishStatusAsync(
                    command.ParkmeterId,
                    MeterStatus.IN_SERVICE.ToString());

                await _notificationService.NotifyStatusChanged(
                    command.ParkmeterId,
                    MeterStatus.IN_SERVICE.ToString());

                Console.WriteLine(
                    $"{parkmeter.SerialNumber} " +
                    $"auto-activated : IN_SERVICE");
            }
            else
            {
                // just push current status 
                await _mqttPublisher.PublishStatusAsync(
                    command.ParkmeterId,
                    parkmeter.Status.ToString());

                Console.WriteLine(
                    $"[ONLINE] {parkmeter.SerialNumber} " +
                    $"→ status: {parkmeter.Status}");
            }

            return true;
        }
    }
}