using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Application.Interfaces.Services;
using parkmeter_management.Domain.Entities;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Application.Commands.MachineEvents.HandleDoorClosed
{
    public class HandleDoorClosedCommandHandler
        : IRequestHandler<HandleDoorClosedCommand, bool>
    {
        private readonly IAlertRepository _alertRepository;
        private readonly IParkmeterRepository _parkmeterRepository;
        private readonly IMessagePublisherService _mqttPublisher;

        public HandleDoorClosedCommandHandler(
            IAlertRepository alertRepository,
            IParkmeterRepository parkmeterRepository,
            IMessagePublisherService mqttPublisher)
        {
            _alertRepository = alertRepository;
            _parkmeterRepository = parkmeterRepository;
            _mqttPublisher = mqttPublisher;
        }

        public async Task<bool> Handle(
            HandleDoorClosedCommand command,
            CancellationToken ct)
        {
            
            // aut-resolve DOOR_OPEN
            var alerts = await _alertRepository
                .GetByParkmeterAsync(command.ParkmeterId);
            foreach (var a in alerts.Where(a =>
                a.Status == AlertStatus.NOT_RESOLVED &&
                a.Type == AlertType.DOOR_OPEN))
            {
                a.Status = AlertStatus.RESOLVED;
                a.ResolvedAt = DateTime.UtcNow;
                await _alertRepository.UpdateAsync(a);
                Console.WriteLine("Auto-resolved: DOOR_OPEN");
            }

            // check stillBlocked
            var remaining = await _alertRepository
                .GetByParkmeterAsync(command.ParkmeterId);
            var stillBlocked = remaining.Any(a =>
                a.Status == AlertStatus.NOT_RESOLVED &&
                (a.Type == AlertType.CASH_BOX_FULL ||
                 a.Type == AlertType.PAPER_EMPTY ||
                 a.Type == AlertType.HARDWARE_FAILURE));

            // if not blocked revert to IN_SERVICE 
            var parkmeter = await _parkmeterRepository
                .GetByIdAsync(command.ParkmeterId);
            if (!stillBlocked && parkmeter != null &&
                parkmeter.Status == MeterStatus.TEMPORARILY_UNAVAILABLE)
            {
                parkmeter.Status = MeterStatus.IN_SERVICE;
                await _parkmeterRepository.UpdateAsync(parkmeter);
                await _mqttPublisher.PublishStatusAsync(
                    command.ParkmeterId,
                    MeterStatus.IN_SERVICE.ToString());
                Console.WriteLine(
                    $" {parkmeter.SerialNumber} : IN_SERVICE (door closed)");
            }

            return true;
        }
    }
}