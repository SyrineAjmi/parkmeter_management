using MediatR;

namespace parkmeter_management.Application.Commands.Mqtt.HandleTransactions
{
    public class HandleTransactionMqttCommand : IRequest<bool>
    {
        public string Payload { get; set; } = string.Empty;
    }
}