using parkmeter_management.Infrastructure.Mqtt;
using System.Net.Sockets;

namespace parkmeter_management.Api.BackgroundServices
{
    public class MqttBackgroundService : BackgroundService
    {
        private readonly MqttBrokerService _broker;
        private readonly MqttClientService _client;

        public MqttBackgroundService(
            MqttBrokerService broker,
            MqttClientService client)
        {
            _broker = broker;
            _client = client;
        }

        private async Task WaitForMqtt(int port, int timeoutMs = 10000)
        {
            var start = DateTime.UtcNow;

            while ((DateTime.UtcNow - start).TotalMilliseconds < timeoutMs)
            {
                try
                {
                    using var client = new TcpClient();
                    await client.ConnectAsync("localhost", port);
                    return;
                }
                catch
                {
                    await Task.Delay(300);
                }
            }

            throw new Exception("MQTT broker failed to start on port " + port);
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            await _broker.StartAsync();
            await WaitForMqtt(1883);
            await _client.StartAsync(stoppingToken);

            Console.WriteLine("[MQTT] Ready");
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        public override async Task StopAsync(
            CancellationToken cancellationToken)
        {
            await _client.StopAsync();
            await _broker.StopAsync();
            await base.StopAsync(cancellationToken);
        }
    }
}