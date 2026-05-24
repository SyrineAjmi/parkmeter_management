using MQTTnet;
using MQTTnet.Server;
using System.Net;

namespace parkmeter_management.Infrastructure.Mqtt
{
    public class MqttBrokerService
    {
        private MqttServer? _mqttServer;

        public async Task StartAsync()
        {
            var serverOptions = new MqttServerOptionsBuilder()
                //.WithDefaultEndpoint()
                .WithDefaultEndpointBoundIPAddress(IPAddress.Any)
                .WithDefaultEndpointPort(1883)
                .Build();

            _mqttServer = new MqttFactory()
                .CreateMqttServer(serverOptions);

            _mqttServer.ClientConnectedAsync += e =>
            {
                Console.WriteLine(
                    $"[BROKER] Client connected: {e.ClientId}");
                return Task.CompletedTask;
            };

            _mqttServer.ClientDisconnectedAsync += e =>
            {
                Console.WriteLine(
                    $"[BROKER] Client disconnected: {e.ClientId}");
                return Task.CompletedTask;
            };

            try
            {
                await _mqttServer.StartAsync();
                Console.WriteLine("[BROKER] Running on port 1883");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BROKER ERROR] Failed to start: {ex.Message}");
                throw;
            }
        }

        public async Task StopAsync()
        {
            if (_mqttServer != null)
                await _mqttServer.StopAsync();

            Console.WriteLine("[BROKER] Stopped");
        }
    }
}