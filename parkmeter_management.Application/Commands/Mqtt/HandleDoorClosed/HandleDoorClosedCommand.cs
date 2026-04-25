using MediatR;

namespace parkmeter_management.Application.Commands.Mqtt.HandleDoorClosed
{
    public class HandleDoorClosedCommand : IRequest<bool>
    {
        public string Payload { get; set; } = string.Empty;
    }
}