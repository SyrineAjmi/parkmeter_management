using parkmeter_management.Infrastructure.Mqtt;

namespace parkmeter_management.Api.BackgroundServices
{
    public class MqttBackgroundService : BackgroundService
    {
        private readonly MqttBrokerService _broker;
        private readonly MqttSubscriberService _subscriber;

        public MqttBackgroundService(
            MqttBrokerService broker,
            MqttSubscriberService subscriber)
        {
            _broker = broker;
            _subscriber = subscriber;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _broker.StartAsync();
            await _subscriber.StartAsync();
            Console.WriteLine("MQTT Background Service running");
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await _subscriber.StopAsync();
            await _broker.StopAsync();
            Console.WriteLine("MQTT Background Service stopped");
        }
    }
}