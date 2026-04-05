using MediatR;
using parkmeter_management.Application.Helpers;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Application.Interfaces.Services;
using parkmeter_management.Domain.Entities;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Application.Commands.Alerts
{
    public class CreateAlertCommandHandler
        : IRequestHandler<CreateAlertCommand, Guid>
    {
        private readonly IAlertRepository _alertRepository;
        private readonly IParkmeterRepository _parkmeterRepository;
        private readonly ISessionRepository _sessionRepository;
        private readonly IMqttPublisherService _mqttPublisher;

        public CreateAlertCommandHandler(
            IAlertRepository alertRepository,
            IParkmeterRepository parkmeterRepository,
            ISessionRepository sessionRepository,
            IMqttPublisherService mqttPublisher)
        {
            _alertRepository = alertRepository;
            _parkmeterRepository = parkmeterRepository;
            _sessionRepository = sessionRepository;
            _mqttPublisher = mqttPublisher;
        }

        public async Task<Guid> Handle(
            CreateAlertCommand command, CancellationToken ct)
        {
            // ── Get parkmeter ─────────────────────────
            var parkmeter = await _parkmeterRepository
                .GetByIdAsync(command.ParkmeterId);
            if (parkmeter == null) return Guid.Empty;

            // ── Operational alerts only when IN_SERVICE ─
            var operationalAlerts =
                AlertType.CASH_BOX_WARN |
                AlertType.CASH_BOX_FULL |
                AlertType.PAPER_LOW |
                AlertType.PAPER_EMPTY;

            if ((command.Type & operationalAlerts) != AlertType.NONE
                && parkmeter.Status != MeterStatus.IN_SERVICE
                && parkmeter.Status != MeterStatus.TEMPORARILY_UNAVAILABLE)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"⚠️ Alert {command.Type} ignored — " +
                    $"{parkmeter.SerialNumber} is {parkmeter.Status}");
                Console.ResetColor();
                return Guid.Empty;
            }

            // ── Blocking alerts → TEMPORARILY_UNAVAILABLE
            var blockingAlerts =
                AlertType.CASH_BOX_FULL |
                AlertType.PAPER_EMPTY |
                AlertType.DOOR_OPEN |
                AlertType.HARDWARE_FAILURE; 

            if ((command.Type & blockingAlerts) != AlertType.NONE
                && parkmeter.Status == MeterStatus.IN_SERVICE)
            {
                parkmeter.Status = MeterStatus.TEMPORARILY_UNAVAILABLE;
                await _parkmeterRepository.UpdateAsync(parkmeter);
                await _mqttPublisher.PublishStatusAsync(
                    command.ParkmeterId,
                    MeterStatus.TEMPORARILY_UNAVAILABLE.ToString());

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"⚠️ {parkmeter.SerialNumber} → " +
                    $"TEMPORARILY_UNAVAILABLE ({command.Type})");
                Console.ResetColor();
            }

            // ── POWER_FAILURE → OUT_OF_SERVICE ────────
            if (command.Type.HasFlag(AlertType.POWER_FAILURE))
            {
                parkmeter.Status = MeterStatus.OUT_OF_SERVICE;
                await _parkmeterRepository.UpdateAsync(parkmeter);
                await _mqttPublisher.PublishStatusAsync(
                    command.ParkmeterId,
                    MeterStatus.OUT_OF_SERVICE.ToString());

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"⚡ {parkmeter.SerialNumber} → OUT_OF_SERVICE");
                Console.ResetColor();
            }


            // ── Save alert ────────────────────────────
            var alert = new Alert
            {
                Id = Guid.NewGuid(),
                ParkmeterId = command.ParkmeterId,
                Type = command.Type,
                Priority = AlertPriorityHelper.GetPriority(command.Type),
                Description = command.Description,
                Status = AlertStatus.NOT_RESOLVED,
                CreatedAt = DateTime.UtcNow
            };

            await _alertRepository.AddAsync(alert);
            return alert.Id;
        }
    }
}