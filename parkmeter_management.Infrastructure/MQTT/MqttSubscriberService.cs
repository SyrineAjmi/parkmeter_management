using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using MQTTnet.Client;
using parkmeter_management.Application.Commands.Mqtt.HandleAlerts;
using parkmeter_management.Application.Commands.Mqtt.HandleEvents;
using parkmeter_management.Application.Commands.Mqtt.HandleReconnected;
using parkmeter_management.Application.Commands.Mqtt.HandleTransactions;
using parkmeter_management.Application.Commands.Mqtt.HandleParkmeterOnline;

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
            var payload = e.ApplicationMessage.ConvertPayloadToString();

            if (topic.EndsWith("/alerts"))
                await RouteAsync(new HandleAlertMqttCommand
                { Payload = payload });

            else if (topic.EndsWith("/events"))
                await RouteAsync(new HandleEventMqttCommand
                { Payload = payload });

            else if (topic.EndsWith("/transactions"))
                await RouteAsync(new HandleTransactionMqttCommand
                { Payload = payload });

            else if (topic.EndsWith("/reconnected"))
                await RouteAsync(new HandleReconnectedCommand
                { Payload = payload });

            else if (topic.EndsWith("/online"))
                await RouteAsync(new HandleParkmeterOnlineCommand
                { Payload = payload });

            else
                Console.WriteLine($"[MQTT] Unknown topic: {topic}");
        };

        await _client.ConnectAsync(clientOptions);

        // device → backend
        await _client.SubscribeAsync("parkmeters/+/alerts");
        await _client.SubscribeAsync("parkmeters/+/events");       
        await _client.SubscribeAsync("parkmeters/+/transactions");
        await _client.SubscribeAsync("parkmeters/+/reconnected");
        await _client.SubscribeAsync("parkmeters/+/online");

        Console.WriteLine("Subscribed to parkmeter topics");
    }

    private async Task RouteAsync(IRequest<bool> command)
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