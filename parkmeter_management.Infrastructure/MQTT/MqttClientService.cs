using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using parkmeter_management.Application.Interfaces.Services;
using parkmeter_management.Domain.Entities;
using parkmeter_management.Domain.Enums;
using System.Text.Json;

namespace parkmeter_management.Infrastructure.Mqtt
{
    public class MqttClientService : IMessagePublisherService
    {
        private IMqttClient? _client;
        private readonly IServiceProvider _serviceProvider;

        public MqttClientService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }


        public async Task StartAsync(CancellationToken ct = default)
        {
            _client = new MqttFactory().CreateMqttClient();

            var options = new MqttClientOptionsBuilder()
                .WithTcpServer("localhost", 1883)
                .WithClientId("parkmeter-server-client")
                .Build();

            _client.ApplicationMessageReceivedAsync +=
                OnMessageReceivedAsync;

            await _client.ConnectAsync(options, ct);

            await _client.SubscribeAsync("parkmeters/+/events");
            await _client.SubscribeAsync("parkmeters/+/alerts");
            await _client.SubscribeAsync("parkmeters/+/transactions");

            Console.WriteLine("[MQTT] Client connected and subscribed");
        }

        public async Task StopAsync()
        {
            if (_client?.IsConnected == true)
                await _client.DisconnectAsync();

            Console.WriteLine("[MQTT] Client disconnected");
        }

        //  

        private async Task OnMessageReceivedAsync(
            MqttApplicationMessageReceivedEventArgs e)
        {
            var topic = e.ApplicationMessage.Topic;
            var payload = e.ApplicationMessage
                .ConvertPayloadToString();

            if (string.IsNullOrEmpty(payload)) return;

            try
            {
                IRequest<bool>? command = null;

                if (topic.EndsWith("/alerts"))
                    command = MqttCommandMapper.MapAlert(payload);
                else if (topic.EndsWith("/transactions"))
                    command = MqttCommandMapper.MapTransaction(payload);
                else if (topic.EndsWith("/events"))
                    command = MqttCommandMapper.MapMachineEvent(payload);

                if (command == null) return;

                using var scope = _serviceProvider.CreateScope();
                var mediator = scope.ServiceProvider
                    .GetRequiredService<IMediator>();
                await mediator.Send(command);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[MQTT ERROR] {topic}: {ex.Message}");
            }
        }

        // 

        public async Task PublishConfigAsync(
            Guid parkmeterId, ParkMeterConfig config)
        {
            object parameters = config.StrategyType switch
            {
                PricingStrategyType.FLAT_RATE => new
                {
                    pricePerHour = config.PricePerHour
                },
                PricingStrategyType.TIME_OF_DAY => new
                {
                    peakRate = config.PeakRate,
                    offPeakRate = config.OffPeakRate
                },
                PricingStrategyType.WEEKDAY_WEEKEND => new
                {
                    weekdayRate = config.WeekdayRate,
                    weekendRate = config.WeekendRate
                },
                _ => new { pricePerHour = config.PricePerHour }
            };

            await PublishAsync(
                $"parkmeters/{parkmeterId}/config",
                new
                {
                    parkmeterId = parkmeterId.ToString(),
                    maxDuration = config.MaxDuration,
                    pricing = new
                    {
                        strategy = config.StrategyType.ToString(),
                        parameters
                    },
                    sentAt = DateTime.UtcNow
                },
                retain: true);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(
                $"[MQTT] Config pushed → {parkmeterId} " +
                $"| Strategy: {config.StrategyType}");
            Console.ResetColor();
        }

        public async Task PublishStatusAsync(
            Guid parkmeterId, string status)
        {
            await PublishAsync(
                $"parkmeters/{parkmeterId}/status",
                status,
                retain: true);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(
                $"[MQTT] Status pushed → {parkmeterId} : {status}");
            Console.ResetColor();
        }

        // 

        private async Task PublishAsync(
            string topic, object payload, bool retain = false)
        {
            if (_client?.IsConnected != true)
            {
                Console.WriteLine(
                    "[MQTT WARN] Cannot publish — client not connected");
                return;
            }

            var json = payload is string s
                ? s
                : JsonSerializer.Serialize(payload);

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(json)
                .WithQualityOfServiceLevel(
                    MqttQualityOfServiceLevel.AtLeastOnce)
                .WithRetainFlag(retain)
                .Build();

            await _client.PublishAsync(message);
        }
    }
}