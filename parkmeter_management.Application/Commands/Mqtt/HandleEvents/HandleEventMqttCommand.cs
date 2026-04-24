using MediatR;

namespace parkmeter_management.Application.Commands.Mqtt.HandleEvents
{
    public class HandleEventMqttCommand : IRequest<bool>
    {
        public string Payload { get; set; } = string.Empty;
    }
}