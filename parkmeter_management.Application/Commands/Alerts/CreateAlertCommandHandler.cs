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
        private readonly INotificationService _notificationService;

        public CreateAlertCommandHandler(
            IAlertRepository alertRepository,
            IParkmeterRepository parkmeterRepository,
            ISessionRepository sessionRepository,
            INotificationService notificationService)
        {
            _alertRepository = alertRepository;
            _parkmeterRepository = parkmeterRepository;
            _sessionRepository = sessionRepository;
            _notificationService = notificationService;
        }

        public async Task<Guid> Handle(
            CreateAlertCommand command, CancellationToken ct)
        {
            var parkmeter = await _parkmeterRepository
                .GetByIdAsync(command.ParkmeterId);
            if (parkmeter == null) return Guid.Empty;

            // check duplicates
            var existingAlerts = await _alertRepository
                .GetByParkmeterAsync(command.ParkmeterId);
            var isDuplicate = existingAlerts.Any(a =>
                a.Type == command.Type &&
                a.Status == AlertStatus.NOT_RESOLVED);
            if (isDuplicate) return Guid.Empty;

            // operational alerts only for IN_SERVICE or TEMPORARILY_UNAVAILABLE
            var operationalAlerts =
                AlertType.CASH_BOX_WARN |
                AlertType.CASH_BOX_FULL |
                AlertType.PAPER_LOW |
                AlertType.PAPER_EMPTY;

            if ((command.Type & operationalAlerts) != AlertType.NONE
                && parkmeter.Status != MeterStatus.IN_SERVICE
                && parkmeter.Status != MeterStatus.TEMPORARILY_UNAVAILABLE)
                return Guid.Empty;

            // thses alerts — reject if PENDING / OUT_OF_SERVICE
            var nonOperationalAlerts =
                AlertType.HARDWARE_FAILURE |
                AlertType.DOOR_OPEN |
                AlertType.CONNECTION_LOST;

            if ((command.Type & nonOperationalAlerts) != AlertType.NONE)
            {
                if (parkmeter.Status == MeterStatus.PENDING_ACTIVATION)
                {
                    Console.WriteLine(
                        $"[ALERT] {command.Type} rejected — " +
                        $"parkmeter status: {parkmeter.Status}");
                    return Guid.Empty;
                }
            }


            // save alert — no status push, machine already reported it
            var alert = new Alert(command.ParkmeterId, command.Type, command.Description, AlertPriorityHelper.GetPriority(command.Type));


            await _alertRepository.AddAsync(alert);
            await _notificationService.NotifyNewAlert(
                command.ParkmeterId,
                parkmeter.SerialNumber,
                command.Type.ToString(),
                AlertPriorityHelper.GetPriority(command.Type).ToString());

            Console.WriteLine(
                $"[ALERT] {command.Type} saved " +
                $"(Priority: {alert.Priority}) → {alert.Id}");

            return alert.Id;
        }
    }
}