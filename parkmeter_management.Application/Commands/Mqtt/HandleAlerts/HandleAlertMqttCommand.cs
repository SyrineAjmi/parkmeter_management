using MediatR;

namespace parkmeter_management.Application.Commands.Mqtt.HandleAlerts
{
    public class HandleAlertMqttCommand : IRequest<bool>
    {
        public string Payload { get; set; } = string.Empty;
    }
}