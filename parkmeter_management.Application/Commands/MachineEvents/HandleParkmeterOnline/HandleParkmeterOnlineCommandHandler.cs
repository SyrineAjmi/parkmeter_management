using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Application.Interfaces.Services;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Application.Commands.MachineEvents.HandleParkmeterOnline
{
    public class HandleParkmeterOnlineCommandHandler
        : IRequestHandler<HandleParkmeterOnlineCommand, bool>
    {
        private readonly IParkmeterRepository _parkmeterRepository;
        private readonly IParkmeterConfigurationRepository _configurationRepository;
        private readonly IMessagePublisherService _mqttPublisher;
        private readonly INotificationService _notificationService;

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

            // push config — backend's responsibility
            if (config != null)
                await _mqttPublisher.PublishConfigAsync(
                    command.ParkmeterId, config);

            // no status push — machine receives config,
            // evaluates its conditions, and reports its own status
            // via parkmeters/{id}/status topic

            await _notificationService.NotifyParkmeterOnline(
                command.ParkmeterId,
                parkmeter.SerialNumber
            );

            return true;
        }
    }
}