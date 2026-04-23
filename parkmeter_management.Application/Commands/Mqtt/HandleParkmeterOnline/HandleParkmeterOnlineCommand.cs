using MediatR;

namespace parkmeter_management.Application.Commands.Mqtt.HandleParkmeterOnline
{
    public class HandleParkmeterOnlineCommand
        : IRequest<bool>
    {
        public string Payload { get; set; } = string.Empty;
    }
}