using MediatR;
using parkmeter_management.Application.Commands.Alerts;
using parkmeter_management.Application.Helpers;
using parkmeter_management.Domain.Enums;
using System.Text.Json;

namespace parkmeter_management.Application.Commands.Mqtt.HandleAlerts
{
    public class HandleAlertMqttCommandHandler
        : IRequestHandler<HandleAlertMqttCommand, bool>
    {
        private readonly IMediator _mediator;

        public HandleAlertMqttCommandHandler(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task<bool> Handle(
            HandleAlertMqttCommand command,
            CancellationToken ct)
        {
            var raw = JsonSerializer
                .Deserialize<JsonElement>(command.Payload);
            var parkmeterId = Guid.Parse(
                raw.GetProperty("ParkmeterId").GetString()!);
            var alertValue = raw.GetProperty("AlertType").GetInt32();
            var combinedType = (AlertType)alertValue;

            foreach (AlertType type in Enum.GetValues<AlertType>())
            {
                if (type == AlertType.NONE) continue;
                if (!combinedType.HasFlag(type)) continue;

                var alertId = await _mediator.Send(
                    new CreateAlertCommand
                    {
                        ParkmeterId = parkmeterId,
                        Type = type,
                        Description = $"{type} detected"
                    });

                Console.WriteLine($"Alert saved: {type} " +
                    $"(Priority: {AlertPriorityHelper.GetPriority(type)}) " +
                    $"→ {alertId}");
            }

            return true;
        }
    }
}