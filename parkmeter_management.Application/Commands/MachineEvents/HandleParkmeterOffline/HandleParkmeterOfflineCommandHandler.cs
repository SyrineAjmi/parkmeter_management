using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Application.Interfaces.Services;
using parkmeter_management.Domain.Entities;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Application.Commands.MachineEvents.HandleParkmeterOffline
{
    public class HandleParkmeterOfflineCommandHandler
        : IRequestHandler<HandleParkmeterOfflineCommand, bool>
    {
        private readonly IParkmeterRepository _parkmeterRepository;
        private readonly IAlertRepository _alertRepository;
        private readonly INotificationService _notificationService;

        public HandleParkmeterOfflineCommandHandler(
            IParkmeterRepository parkmeterRepository,
            IAlertRepository alertRepository,
            INotificationService notificationService)
        {
            _parkmeterRepository = parkmeterRepository;
            _alertRepository = alertRepository;
            _notificationService = notificationService;
        }
        public async Task<bool> Handle(
        HandleParkmeterOfflineCommand command,
        CancellationToken ct)
        {
            var parkmeter = await _parkmeterRepository
                .GetByIdAsync(command.ParkmeterId);
            if (parkmeter == null) return false;

            Console.WriteLine(
                $"[OFFLINE] {parkmeter.SerialNumber} went offline");

            await _notificationService.NotifyStatusChanged(
                command.ParkmeterId,
                parkmeter.SerialNumber,
                MeterStatus.OUT_OF_SERVICE.ToString());

            return true;
        }

    }
}