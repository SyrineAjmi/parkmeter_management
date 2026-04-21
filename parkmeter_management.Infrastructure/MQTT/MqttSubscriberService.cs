using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using MQTTnet.Client;
using parkmeter_management.Application.Commands.Mqtt.HandleAlerts;
using parkmeter_management.Application.Commands.Mqtt.HandleCashRemoved;
using parkmeter_management.Application.Commands.Mqtt.HandleDoorClosed;
using parkmeter_management.Application.Commands.Mqtt.HandlePaperRefilled;
using parkmeter_management.Application.Commands.Mqtt.HandleReconnected;
using parkmeter_management.Application.Commands.Mqtt.HandleTransactions;

public class MqttSubscriberService
{
    private IMqttClient? _client;
    private readonly IServiceProvider _serviceProvider;

    public MqttSubscriberService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync()
    {
        _client = new MqttFactory().CreateMqttClient();

        var clientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer("localhost", 1883)
            .WithClientId("orchestrator-subscriber")
            .Build();

        _client.ApplicationMessageReceivedAsync += async e =>
        {
            var topic = e.ApplicationMessage.Topic;
            var payload = e.ApplicationMessage
                .ConvertPayloadToString();

            if (topic.EndsWith("/alerts"))
                await RouteAsync(payload,
                    new HandleAlertMqttCommand
                    { Payload = payload });
            else if (topic.EndsWith("/transactions"))
                await RouteAsync(payload,
                    new HandleTransactionMqttCommand
                    { Payload = payload });
            else if (topic.EndsWith("/cash-removed"))
                await RouteAsync(payload,
                    new HandleCashRemovedCommand
                    { Payload = payload });
            else if (topic.EndsWith("/paper-refilled"))
                await RouteAsync(payload,
                    new HandlePaperRefilledCommand
                    { Payload = payload });
            else if (topic.EndsWith("/door-closed"))
                await RouteAsync(payload,
                    new HandleDoorClosedCommand
                    { Payload = payload });
            else if (topic.EndsWith("/reconnected"))
                await RouteAsync(payload,
                    new HandleReconnectedCommand
                    { Payload = payload });
            else
                Console.WriteLine($"Unknown topic: {topic}");
        };

        await _client.ConnectAsync(clientOptions);
        await _client.SubscribeAsync("parkmeters/+/alerts");
        await _client.SubscribeAsync("parkmeters/+/transactions");
        await _client.SubscribeAsync("parkmeters/+/cash-removed");
        await _client.SubscribeAsync("parkmeters/+/paper-refilled");
        await _client.SubscribeAsync("parkmeters/+/door-closed");
        await _client.SubscribeAsync("parkmeters/+/reconnected");

        Console.WriteLine("Subscribed to parkmeter topics");
    }

    private async Task RouteAsync(
        string payload, IRequest<bool> command)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider
                .GetRequiredService<IMediator>();
            await mediator.Send(command);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Routing: {ex.Message}");
        }
    }

    public async Task StopAsync()
    {
        if (_client != null)
            await _client.DisconnectAsync();
    }
}