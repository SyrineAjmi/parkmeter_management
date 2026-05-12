using MediatR;
using parkmeter_management.Application.Commands.Alerts;
using parkmeter_management.Application.Helpers;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Application.Commands.MachineEvents.HandleAlerts
{
    public class HandleAlertCommandHandler
        : IRequestHandler<HandleAlertCommand, bool>
    {
        private readonly IMediator _mediator;

        public HandleAlertCommandHandler(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task<bool> Handle(
            HandleAlertCommand command,
            CancellationToken ct)
        {
            var combinedType = (AlertType)command.AlertType;

            foreach (AlertType type in Enum.GetValues<AlertType>())
            {
                if (type == AlertType.NONE) continue;
                if (!combinedType.HasFlag(type)) continue;

                var alertId = await _mediator.Send(
                    new CreateAlertCommand
                    {
                        ParkmeterId = command.ParkmeterId,
                        Type = type,
                        Description = $"{type} detected"
                    }, ct);

                Console.WriteLine(
                    $"Alert saved: {type} " +
                    $"(Priority: {AlertPriorityHelper.GetPriority(type)}) " +
                    $"→ {alertId}");
            }

            return true;
        }
    }
}