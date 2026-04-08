using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Application.Interfaces.Services;
using parkmeter_management.Domain.Entities;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Application.Commands.Alerts
{
    public class ResolveAlertCommandHandler
        : IRequestHandler<ResolveAlertCommand, bool>
    {
        private readonly IAlertRepository _alertRepository;
        private readonly IParkmeterRepository _parkmeterRepository;
        private readonly IMqttPublisherService _mqttPublisher;



        public ResolveAlertCommandHandler(
            IAlertRepository alertRepository,
            IParkmeterRepository parkmeterRepository,
            IMqttPublisherService mqttPublisher)
        {
            _alertRepository = alertRepository;
            _parkmeterRepository = parkmeterRepository;
            _mqttPublisher = mqttPublisher;

        }

        public async Task<bool> Handle(
        ResolveAlertCommand request, CancellationToken ct)
        {
            var alert = await _alertRepository.GetByIdAsync(request.AlertId);
            if (alert == null) return false;

            // Just mark as resolved
            alert.Status = AlertStatus.RESOLVED;
            alert.ResolvedAt = DateTime.UtcNow;
            await _alertRepository.UpdateAsync(alert);

            // Check if status should revert
            var remaining = await _alertRepository
                .GetByParkmeterAsync(alert.ParkmeterId);

            var stillBlocked = remaining.Any(a =>
                a.Status == AlertStatus.NOT_RESOLVED &&
                (a.Type == AlertType.CASH_BOX_FULL ||
                 a.Type == AlertType.PAPER_EMPTY ||
                 a.Type == AlertType.DOOR_OPEN ||
                 a.Type == AlertType.HARDWARE_FAILURE));

            if (!stillBlocked)
            {
                var parkmeter = await _parkmeterRepository
                    .GetByIdAsync(alert.ParkmeterId);

                if (parkmeter != null &&
                    parkmeter.Status == MeterStatus.TEMPORARILY_UNAVAILABLE)
                {
                    parkmeter.Status = MeterStatus.IN_SERVICE;
                    await _parkmeterRepository.UpdateAsync(parkmeter);
                    await _mqttPublisher.PublishStatusAsync(
                        alert.ParkmeterId,
                        MeterStatus.IN_SERVICE.ToString());

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($" {parkmeter.SerialNumber} → IN_SERVICE");
                    Console.ResetColor();
                }
            }

            return true;
        }
    }
}