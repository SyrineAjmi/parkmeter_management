using MediatR;
using parkmeter_management.Application.Commands.Parkmeters;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Application.Interfaces.Services;
using parkmeter_management.Domain.Enums;
namespace parkmeter_management.Application.Commands.Parkmeters
{
    public class ChangeStatusCommandHandler
    : IRequestHandler<ChangeStatusCommand, bool>
    {
        private readonly IParkmeterRepository _parkmeterRepository;
        private readonly IParkmeterConfigurationRepository _configurationRepository;
        private readonly IMqttPublisherService _mqttPublisher;
        private readonly IAlertRepository _alertRepository;

        public ChangeStatusCommandHandler(
            IParkmeterRepository parkmeterRepository,
            IParkmeterConfigurationRepository configurationRepository,
            IMqttPublisherService mqttPublisher,
            IAlertRepository alertRepository)

        {
            _parkmeterRepository = parkmeterRepository;
            _configurationRepository = configurationRepository;
            _mqttPublisher = mqttPublisher;
            _alertRepository = alertRepository;
        }

        public async Task<bool> Handle(
          ChangeStatusCommand command, CancellationToken ct)
        {
            var parkmeter = await _parkmeterRepository
                .GetByIdAsync(command.ParkmeterId);
            if (parkmeter == null) return false;

            // Cannot set IN_SERVICE without config
            if (command.NewStatus == MeterStatus.IN_SERVICE)
            {
                var config = await _configurationRepository
                    .GetByParkMeterIdAsync(command.ParkmeterId);
                if (config == null) return false;

                // Auto-resolve ALL alerts when operator activates
                var alerts = await _alertRepository
                    .GetByParkmeterAsync(command.ParkmeterId);

                foreach (var a in alerts.Where(a =>
                    a.Status == AlertStatus.NOT_RESOLVED))
                {
                    a.Status = AlertStatus.RESOLVED;
                    a.ResolvedAt = DateTime.UtcNow;
                    await _alertRepository.UpdateAsync(a);
                    Console.WriteLine($"✅ Auto-resolved on activation: {a.Type}");
                }
            }

            parkmeter.Status = command.NewStatus;
            await _parkmeterRepository.UpdateAsync(parkmeter);
            await _mqttPublisher.PublishStatusAsync(
                command.ParkmeterId,
                command.NewStatus.ToString());


            Console.WriteLine($"Status changed: {parkmeter.SerialNumber} " +
        $"→ {command.NewStatus}");

            return true;
        }
    }
}

