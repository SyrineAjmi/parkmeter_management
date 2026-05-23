using parkmeter_management.Infrastructure.Mqtt;

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

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            await _broker.StartAsync();
            await Task.Delay(500, stoppingToken);
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