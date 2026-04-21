using MediatR;

namespace parkmeter_management.Application.Commands.Mqtt.HandleReconnected
{
    public class HandleReconnectedCommand : IRequest<bool>
    {
        public string Payload { get; set; } = string.Empty;
    }
}