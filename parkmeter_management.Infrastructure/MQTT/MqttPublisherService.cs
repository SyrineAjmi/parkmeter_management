using MQTTnet;
using MQTTnet.Client;
using parkmeter_management.Application.Interfaces.Services;
using parkmeter_management.Domain.Enums;
using System.Text.Json;

namespace parkmeter_management.Infrastructure.Mqtt
{
    public class MqttPublisherService : IMqttPublisherService
    {
        private IMqttClient? _client;
        private readonly SemaphoreSlim _connectLock = new(1, 1);

        public async Task EnsureConnectedAsync()
        {
            if (_client != null) return;

            await _connectLock.WaitAsync();
            try
            {
                if (_client != null) return;

                _client = new MqttFactory().CreateMqttClient();
                var options = new MqttClientOptionsBuilder()
                    .WithTcpServer("localhost", 1883)
                    .WithClientId("orchestrator-publisher")
                    .Build();
                await _client.ConnectAsync(options);
                Console.WriteLine("✅ Publisher connected to broker");
            }
            finally
            {
                _connectLock.Release();
            }
        }

        /// <summary>
        /// Pushes configuration to a parkmeter.
        /// Topic: parkmeters/{id}/commands/config
        /// </summary>
        public async Task PublishConfigAsync(
            Guid parkmeterId,
            decimal pricePerHour,
            int maxDuration)
        {
            await EnsureConnectedAsync();

            var message = new MqttApplicationMessageBuilder()
                .WithTopic($"parkmeters/{parkmeterId}/commands/config")
                .WithPayload(JsonSerializer.Serialize(new
                {
                    ParkmeterId = parkmeterId.ToString(),
                    PricePerHour = pricePerHour,
                    MaxDuration = maxDuration,
                    SentAt = DateTime.UtcNow
                }))
                .WithRetainFlag(true)
                .Build();

            await _client!.PublishAsync(message);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(
                $"[CMD] Config → {parkmeterId} " +
                $"({pricePerHour} DT/h | {maxDuration} min)");
            Console.ResetColor();
        }

        /// <summary>
        /// Pushes a status update to a parkmeter.
        /// Topic: parkmeters/{id}/commands/status
        /// </summary>
        public async Task PublishStatusAsync(Guid parkmeterId, string status)
        {
            await EnsureConnectedAsync();

            var message = new MqttApplicationMessageBuilder()
                .WithTopic($"parkmeters/{parkmeterId}/commands/status")
                .WithPayload(status)
                .WithRetainFlag(true)
                .Build();

            await _client!.PublishAsync(message);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[CMD] Status → {parkmeterId} : {status}");
            Console.ResetColor();
        }

        /// <summary>
        /// Publishes an operator/lifecycle event to parkmeters/{id}/events.
        /// Used by SimulatorController so the HTML operator panel goes
        /// through the same MQTT pipeline as real hardware.
        /// </summary>
        public async Task PublishEventAsync(
            Guid parkmeterId,
            ParkMeterEventType eventType)
        {
            await EnsureConnectedAsync();

            var message = new MqttApplicationMessageBuilder()
                .WithTopic($"parkmeters/{parkmeterId}/events")
                .WithPayload(JsonSerializer.Serialize(new
                {
                    ParkmeterId = parkmeterId.ToString(),
                    EventType = (int)eventType,
                    CreatedAt = DateTime.UtcNow
                }))
                .WithRetainFlag(false)
                .Build();

            await _client!.PublishAsync(message);

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"[EVENT] {eventType} → {parkmeterId}");
            Console.ResetColor();
        }

        /// <summary>
        /// Notifies the system that a new parkmeter was registered.
        /// Topic: system/parkmeters/registered
        /// </summary>
        public async Task PublishNewParkmeterAsync(
            Guid parkmeterID, string serialNumber)
        {
            await EnsureConnectedAsync();

            var message = new MqttApplicationMessageBuilder()
                .WithTopic("system/parkmeters/registered")
                .WithPayload(JsonSerializer.Serialize(new
                {
                    Id = parkmeterID.ToString(),
                    SerialNumber = serialNumber
                }))
                .WithRetainFlag(false)
                .Build();

            await _client!.PublishAsync(message);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(
                $"[SYSTEM] New parkmeter registered: {serialNumber}");
            Console.ResetColor();
        }
    }
}