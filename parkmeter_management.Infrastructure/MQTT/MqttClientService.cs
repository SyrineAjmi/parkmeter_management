using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using parkmeter_management.Application.Interfaces.Services;
using parkmeter_management.Domain.Entities;
using parkmeter_management.Domain.Enums;
using System.Text.Json;
using System.Threading.Channels;

namespace parkmeter_management.Infrastructure.Mqtt
{
    public class MqttClientService : IMessagePublisherService
    {
        private IMqttClient? _client;
        private readonly IServiceProvider _serviceProvider;

        // ── Queue ─────────────────────────────────────────────
        private readonly Channel<(string topic, string payload)> _queue
            = Channel.CreateUnbounded<(string, string)>();

        public MqttClientService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            // start background consumer
            _ = Task.Run(ProcessQueueAsync);
        }

        // ── Lifecycle ─────────────────────────────────────────

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

            // machine - backend topics
            await _client.SubscribeAsync("parkmeters/+/events");
            await _client.SubscribeAsync("parkmeters/+/alerts");
            await _client.SubscribeAsync("parkmeters/+/transactions");
            await _client.SubscribeAsync("parkmeters/+/status");
            await _client.SubscribeAsync("parkmeters/+/telemetry");


            Console.WriteLine("[MQTT] Client connected and subscribed");
        }

        public async Task StopAsync()
        {
            if (_client?.IsConnected == true)
                await _client.DisconnectAsync();

            Console.WriteLine("[MQTT] Client disconnected");
        }

        // ── Inbound — map and route to MediatR ───────────────

        private Task OnMessageReceivedAsync(
        MqttApplicationMessageReceivedEventArgs e)
        {
            var topic = e.ApplicationMessage.Topic;
            var payload = e.ApplicationMessage.ConvertPayloadToString();

            if (!string.IsNullOrEmpty(payload))
                _queue.Writer.TryWrite((topic, payload));

            return Task.CompletedTask; 
        }

        private async Task ProcessQueueAsync()
        {
            await foreach (var (topic, payload) in _queue.Reader.ReadAllAsync())
            {
                try
                {
                    IRequest<bool>? command = null;

                    if (topic.EndsWith("/alerts"))
                        command = MqttCommandMapper.MapAlert(payload);
                    else if (topic.EndsWith("/transactions"))
                        command = MqttCommandMapper.MapTransaction(payload);
                    else if (topic.EndsWith("/events"))
                        command = MqttCommandMapper.MapMachineEvent(payload);
                    else if (topic.EndsWith("/status"))
                        command = MqttCommandMapper.MapStatus(payload);
                    else if (topic.EndsWith("/telemetry"))
                        command = MqttCommandMapper.MapTelemetry(payload);

                    if (command == null) continue;

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
        }


        //private async Task OnMessageReceivedAsync(
        //    MqttApplicationMessageReceivedEventArgs e)
        //{
        //    var topic = e.ApplicationMessage.Topic;
        //    var payload = e.ApplicationMessage
        //        .ConvertPayloadToString();

        //    if (string.IsNullOrEmpty(payload)) return;

        //    try
        //    {
        //        IRequest<bool>? command = null;

        //        if (topic.EndsWith("/alerts"))
        //            command = MqttCommandMapper.MapAlert(payload);
        //        else if (topic.EndsWith("/transactions"))
        //            command = MqttCommandMapper.MapTransaction(payload);
        //        else if (topic.EndsWith("/events"))
        //            command = MqttCommandMapper.MapMachineEvent(payload);
        //        else if (topic.EndsWith("/status"))
        //            command = MqttCommandMapper.MapStatus(payload);
        //        else if (topic.EndsWith("/telemetry"))
        //            command = MqttCommandMapper.MapTelemetry(payload);

        //        if (command == null) return;

        //        using var scope = _serviceProvider.CreateScope();
        //        var mediator = scope.ServiceProvider
        //            .GetRequiredService<IMediator>();
        //        await mediator.Send(command);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(
        //            $"[MQTT ERROR] {topic}: {ex.Message}");
        //    }
        //}

        // ── Outbound — publish to hardware/simulator ─────────

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

            if (_client?.IsConnected != true)
            {
                Console.WriteLine(
                    "[MQTT WARN] Cannot publish status — client not connected");
                return;
            }

            await PublishAsync(
                $"parkmeters/{parkmeterId}/command",
                new
                {
                    command = $"SET_{status}",
                    sentAt = DateTime.UtcNow
                },
                retain: false);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(
                $"[MQTT] Command sent → {parkmeterId} : SET_{status}");
            Console.ResetColor();
        }

        public async Task<bool> PublishCommandAsync(
            Guid parkmeterId, string command)
        {
            if (_client?.IsConnected != true)
            {
                Console.WriteLine(
                    $"[MQTT WARN] Cannot publish command — " +
                    $"client not connected");
                return false;
            }

            await PublishAsync(
                $"parkmeters/{parkmeterId}/command",
                new
                {
                    command = command,
                    sentAt = DateTime.UtcNow
                },
                retain: false);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(
                $"[MQTT] Command sent → {parkmeterId} : {command}");
            Console.ResetColor();

            return true;
        }

        // ── Shared publish helper ─────────────────────────────

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
