using MediatR;

namespace parkmeter_management.Application.Commands.Mqtt.HandlePaperRefilled
{
    public class HandlePaperRefilledCommand : IRequest<bool>
    {
        public string Payload { get; set; } = string.Empty;
    }
}