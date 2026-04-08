using MQTTnet;
using MQTTnet.Client;
using parkmeter_management.Application.Interfaces.Services;
using System.Text.Json;

namespace parkmeter_management.Infrastructure.Mqtt
{
    public class MqttPublisherService : IMqttPublisherService
    {
        private IMqttClient? _client;

        public async Task EnsureConnectedAsync()
        {
            if (_client == null)
            {
                _client = new MqttFactory().CreateMqttClient();
                var options = new MqttClientOptionsBuilder()
                    .WithTcpServer("localhost", 1883)
                    .WithClientId("orchestrator-publisher")
                    .Build();
                await _client.ConnectAsync(options);
                Console.WriteLine("Publisher connected to broker");
            } 
        }

        public async Task PublishConfigAsync(
            Guid parkmeterId,
            decimal pricePerHour,
            int maxDuration)
        {
            await EnsureConnectedAsync();

            var config = new
            {
                ParkmeterId = parkmeterId.ToString(),
                PricePerHour = pricePerHour,
                MaxDuration = maxDuration,
                SentAt = DateTime.UtcNow
            };

            var message = new MqttApplicationMessageBuilder()
                .WithTopic($"parkmeters/{parkmeterId}/config")
                .WithPayload(JsonSerializer.Serialize(config))
                .WithRetainFlag(false)
                .Build();

            await _client!.PublishAsync(message);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Config pushed to parkmeter {parkmeterId}");
            Console.ResetColor();
        }

        public async Task PublishStatusAsync(Guid parkmeterId, string status)
        {
            await EnsureConnectedAsync();

            var message = new MqttApplicationMessageBuilder()
                .WithTopic($"parkmeters/{parkmeterId}/status")
                .WithPayload(status)
                .WithRetainFlag(true) // retain so parkmeter gets it when he connects
                .Build();

            await _client!.PublishAsync(message);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Status pushed: {parkmeterId} → {status}");
            Console.ResetColor();
        
        }

        public async Task PublishNewParkmeterAsync(
        Guid parkmeterID, string serialNumber)
        {
            await EnsureConnectedAsync();

            var message = new MqttApplicationMessageBuilder()
                .WithTopic("system/parkmeter/new")
                .WithPayload(JsonSerializer.Serialize(new
                {
                    Id = parkmeterID.ToString(),
                    SerialNumber = serialNumber
                }))
                .WithRetainFlag(false) 
                .Build();

            await _client!.PublishAsync(message);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"New parkmeter notification: {serialNumber}");
            Console.ResetColor();
        }
    }


}