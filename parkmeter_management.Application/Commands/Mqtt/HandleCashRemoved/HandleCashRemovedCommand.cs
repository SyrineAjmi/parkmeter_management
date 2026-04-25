using MediatR;

namespace parkmeter_management.Application.Commands.Mqtt.HandleCashRemoved
{
    public class HandleCashRemovedCommand : IRequest<bool>
    {
        public string Payload { get; set; } = string.Empty;
    }

}