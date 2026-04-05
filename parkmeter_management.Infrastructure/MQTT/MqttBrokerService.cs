using MQTTnet;
using MQTTnet.Server;

namespace parkmeter_management.Infrastructure.Mqtt
{
    public class MqttBrokerService
    {
        private MqttServer? _mqttServer;

        public async Task StartAsync()
        {
            var serverOptions = new MqttServerOptionsBuilder()
                .WithDefaultEndpoint()
                .WithDefaultEndpointPort(1883)
                .Build();

            _mqttServer = new MqttFactory().CreateMqttServer(serverOptions);

            _mqttServer.ClientConnectedAsync += e =>
            {
                Console.WriteLine($"Client connected: {e.ClientId}");
                return Task.CompletedTask;
            };

            _mqttServer.InterceptingPublishAsync += e =>
            {
                Console.WriteLine($"[{e.ApplicationMessage.Topic}] " +
                    $"{e.ApplicationMessage.ConvertPayloadToString()}");
                return Task.CompletedTask;
            };

            await _mqttServer.StartAsync();
            Console.WriteLine("Broker running on port 1883");
        }

        public async Task StopAsync()
        {
            if (_mqttServer != null)
                await _mqttServer.StopAsync();
        }
    }
}