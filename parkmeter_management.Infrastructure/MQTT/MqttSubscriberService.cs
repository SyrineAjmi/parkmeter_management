using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using MQTTnet.Client;
using parkmeter_management.Application.Commands.Alerts;
using parkmeter_management.Application.Commands.Transactions;
using parkmeter_management.Application.Helpers;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Application.Interfaces.Services;
using parkmeter_management.Domain.Entities;
using parkmeter_management.Domain.Enums;
using System.Text.Json;

namespace parkmeter_management.Infrastructure.Mqtt
{
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


                // router de messages 
                if (topic.EndsWith("/alerts"))
                    await HandleAlertAsync(payload);
                else if (topic.EndsWith("/transactions"))
                    await HandleTransactionAsync(payload);
                else if (topic.EndsWith("/cash-removed"))
                    await HandleCashRemovedAsync(payload);
                else if (topic.EndsWith("/paper-refilled"))
                    await HandlePaperRefilledAsync(payload);
                else if (topic.EndsWith("/door-closed"))
                    await HandleDoorClosedAsync(payload);
                else if (topic.EndsWith("/reconnected"))
                    await HandleReconnectedAsync(payload);
                else
                    Console.WriteLine($"Unknown topic: {topic}");
            };

            await _client.ConnectAsync(clientOptions);
            await _client.SubscribeAsync("parkmeters/+/alerts");
            await _client.SubscribeAsync("parkmeters/+/transactions");
            await _client.SubscribeAsync("parkmeters/+/cash-removed"); 
            await _client.SubscribeAsync("parkmeters/+/paper-refilled");
            await _client.SubscribeAsync("parkmeters/+/door-closed");
            await _client.SubscribeAsync("parkmeters/+/reconnected");

            Console.WriteLine("Subscribed to parkmeter topics");
        
        }


        // alert 
        private async Task HandleAlertAsync(string payload) 
        {
            try
            {
                var raw = JsonSerializer.Deserialize<JsonElement>(payload);

                var parkmeterId = Guid.Parse(
                    raw.GetProperty("ParkmeterId").GetString()!);
                var alertValue = raw.GetProperty("AlertType").GetInt32();
                var combinedType = (AlertType)alertValue;

                // decodage flags create one alert par type
                foreach (AlertType type in Enum.GetValues<AlertType>())
                {
                    if (type == AlertType.NONE) continue; 

                    if (combinedType.HasFlag(type))
                    {
                        var command = new CreateAlertCommand
                        {
                            ParkmeterId = parkmeterId,
                            Type = type,
                            Description = $"{type} detected"
                        };

                        using var scope = _serviceProvider.CreateScope();
                        var mediator = scope.ServiceProvider
                            .GetRequiredService<IMediator>();

                        var alertId = await mediator.Send(command);
                        Console.WriteLine($" Alert saved: {type} " +
                                            $"(Priority: {AlertPriorityHelper.GetPriority(type)}) " +
                                            $"→ {alertId}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Alert error: {ex.Message}");
                Console.ResetColor();
            }
        }


        // transaction  
        private async Task HandleTransactionAsync(string payload)
        {
            try
            {
                var raw = JsonSerializer.Deserialize<JsonElement>(payload);

                var statusStr = raw.GetProperty("Status").GetString()!;
                Enum.TryParse<TransactionStatus>(statusStr, out var status);

                var command = new CreateTransactionCommand
                {
                    ParkmeterId = Guid.Parse(
                        raw.GetProperty("ParkmeterId").GetString()!),
                    Amount = raw.GetProperty("Amount").GetDecimal(),
                    Duration = raw.GetProperty("Duration").GetInt32(),
                    Status = status,
                };

                using var scope = _serviceProvider.CreateScope();
                var mediator = scope.ServiceProvider
                    .GetRequiredService<IMediator>();
                var transactionId = await mediator.Send(command);

                Console.WriteLine($"Transaction saved: {status} → {transactionId}");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Transaction error: {ex.Message}");
                Console.ResetColor();
            }
        }

        // cashbox
        private async Task HandleCashRemovedAsync(string payload)
        {


            try
            {
                var raw = JsonSerializer.Deserialize<JsonElement>(payload);
                var parkmeterID = Guid.Parse(
                    raw.GetProperty("ParkmeterId").GetString()!);

                using var scope = _serviceProvider.CreateScope();
                var alertRepo = scope.ServiceProvider
                    .GetRequiredService<IAlertRepository>();
                var parkmeterRepo = scope.ServiceProvider
                    .GetRequiredService<IParkmeterRepository>();
                var sessionRepo = scope.ServiceProvider
                    .GetRequiredService<ISessionRepository>();
                var publisher = scope.ServiceProvider
                    .GetRequiredService<IMqttPublisherService>();
                var notificationService = scope.ServiceProvider
                    .GetRequiredService<INotificationService>();

                // 1. Close active session
                var activeSession = await sessionRepo
                    .GetActiveSessionByParkmeterAsync(parkmeterID);

                if (activeSession != null)
                {
                    activeSession.Status = SessionStatus.CLOSED;
                    activeSession.EndDateTime = DateTime.UtcNow;
                    await sessionRepo.UpdateAsync(activeSession);
                    Console.WriteLine($"📋 Session closed | " +
                        $"Total: {activeSession.TotalAmount} DT");

                    //notifying after close
                    await notificationService.NotifySessionClosed(
                        parkmeterID,
                        activeSession.TotalAmount,
                        activeSession.TotalTransactions);
                }

                // 2. Open new session
                await sessionRepo.AddAsync(new Session
                {
                    Id = Guid.NewGuid(),
                    ParkmeterId = parkmeterID,
                    StartDateTime = DateTime.UtcNow,
                    Status = SessionStatus.ACTIVE,
                    TotalTransactions = 0,
                    TotalAmount = 0
                });
                Console.WriteLine($"✅ New session started");
                // 3. Auto-resolve cash AND door alerts
                var alerts = await alertRepo.GetByParkmeterAsync(parkmeterID);
                foreach (var a in alerts.Where(a =>
                    a.Status == AlertStatus.NOT_RESOLVED &&
                    (a.Type == AlertType.CASH_BOX_FULL ||
                     a.Type == AlertType.CASH_BOX_WARN))) 
                {
                    a.Status = AlertStatus.RESOLVED;
                    a.ResolvedAt = DateTime.UtcNow;
                    await alertRepo.UpdateAsync(a);
                    Console.WriteLine($"✅ Auto-resolved: {a.Type}");
                }

                // 4. Re-fetch remaining alerts
                var remaining = await alertRepo
                    .GetByParkmeterAsync(parkmeterID);

                var stillBlocked = remaining.Any(a =>
                    a.Status == AlertStatus.NOT_RESOLVED &&
                    (a.Type == AlertType.PAPER_EMPTY ||
                     a.Type == AlertType.HARDWARE_FAILURE || 
                     a.Type == AlertType.DOOR_OPEN));

                // 5. Revert to IN_SERVICE if not blocked
                var parkmeter = await parkmeterRepo.GetByIdAsync(parkmeterID);
                if (!stillBlocked && parkmeter != null &&
                    parkmeter.Status == MeterStatus.TEMPORARILY_UNAVAILABLE)
                {
                    parkmeter.Status = MeterStatus.IN_SERVICE;
                    await parkmeterRepo.UpdateAsync(parkmeter);
                    await publisher.PublishStatusAsync(
                        parkmeterID,
                        MeterStatus.IN_SERVICE.ToString());

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"✅ {parkmeter.SerialNumber} → IN_SERVICE");
                    Console.ResetColor();
                }

                // 6. Save as resolved alert record
                await alertRepo.AddAsync(new Alert
                {
                    Id = Guid.NewGuid(),
                    ParkmeterId = parkmeterID,
                    Type = AlertType.CASH_BOX_REMOVED,
                    Priority = Priority.NORMAL,
                    Description = "Cash box removed by agent",
                    Status = AlertStatus.RESOLVED,
                    CreatedAt = DateTime.UtcNow,
                    ResolvedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"CashRemoved error: {ex.Message}");
                Console.ResetColor();
            }
        }

        // paper refill
        private async Task HandlePaperRefilledAsync(string payload)
        {
            try
            {
                var raw = JsonSerializer.Deserialize<JsonElement>(payload);
                var parkmeterID = Guid.Parse(
                    raw.GetProperty("ParkmeterId").GetString()!);

                using var scope = _serviceProvider.CreateScope();
                var alertRepo = scope.ServiceProvider
                    .GetRequiredService<IAlertRepository>();
                var parkmeterRepo = scope.ServiceProvider
                    .GetRequiredService<IParkmeterRepository>();
                var publisher = scope.ServiceProvider
                    .GetRequiredService<IMqttPublisherService>();

                // 1. Auto-resolve paper alerts
                var alerts = await alertRepo.GetByParkmeterAsync(parkmeterID);
                foreach (var a in alerts.Where(a =>
                    a.Status == AlertStatus.NOT_RESOLVED &&
                    (a.Type == AlertType.PAPER_EMPTY ||
                     a.Type == AlertType.PAPER_LOW)))  
                {
                    a.Status = AlertStatus.RESOLVED;
                    a.ResolvedAt = DateTime.UtcNow;
                    await alertRepo.UpdateAsync(a);
                    Console.WriteLine($"✅ Auto-resolved: {a.Type}");
                }

                // 2. Re-fetch remaining alerts
                var remaining = await alertRepo.GetByParkmeterAsync(parkmeterID);
                var stillBlocked = remaining.Any(a =>
                    a.Status == AlertStatus.NOT_RESOLVED &&
                    (a.Type == AlertType.CASH_BOX_FULL ||
                     a.Type == AlertType.HARDWARE_FAILURE ||
                     a.Type == AlertType.DOOR_OPEN));


                // 3. Revert to IN_SERVICE if not blocked
                var parkmeter = await parkmeterRepo.GetByIdAsync(parkmeterID);
                if (!stillBlocked && parkmeter != null &&
                    parkmeter.Status == MeterStatus.TEMPORARILY_UNAVAILABLE)
                {
                    parkmeter.Status = MeterStatus.IN_SERVICE;
                    await parkmeterRepo.UpdateAsync(parkmeter);
                    await publisher.PublishStatusAsync(
                        parkmeterID,
                        MeterStatus.IN_SERVICE.ToString());

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"✅ {parkmeter.SerialNumber} → IN_SERVICE (paper refilled)");
                    Console.ResetColor();
                }

                // 4. Save as resolved alert record
                await alertRepo.AddAsync(new Alert
                {
                    Id = Guid.NewGuid(),
                    ParkmeterId = parkmeterID,
                    Type = AlertType.PAPER_REFILLED,
                    Priority = Priority.LOW,
                    Description = "Paper refilled by agent",
                    Status = AlertStatus.RESOLVED,
                    CreatedAt = DateTime.UtcNow,
                    ResolvedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"PaperRefilled error: {ex.Message}");
                Console.ResetColor();
            }
        }

        // door closed
        private async Task HandleDoorClosedAsync(string payload)
        {

            try
            {
                var raw = JsonSerializer.Deserialize<JsonElement>(payload);
                var parkmeterID = Guid.Parse(
                    raw.GetProperty("ParkmeterId").GetString()!);

                using var scope = _serviceProvider.CreateScope();
                var alertRepo = scope.ServiceProvider
                    .GetRequiredService<IAlertRepository>();
                var parkmeterRepo = scope.ServiceProvider
                    .GetRequiredService<IParkmeterRepository>();
                var publisher = scope.ServiceProvider
                    .GetRequiredService<IMqttPublisherService>();

                // Auto-resolve DOOR_OPEN alerts
                var alerts = await alertRepo.GetByParkmeterAsync(parkmeterID);

                foreach (var a in alerts.Where(a =>
                    a.Status == AlertStatus.NOT_RESOLVED &&
                    a.Type == AlertType.DOOR_OPEN))
                {
                    a.Status = AlertStatus.RESOLVED;
                    a.ResolvedAt = DateTime.UtcNow;
                    await alertRepo.UpdateAsync(a);
                    Console.WriteLine($"✅ Auto-resolved: DOOR_OPEN");
                }

                var remaining = await alertRepo.GetByParkmeterAsync(parkmeterID);
                var stillBlocked = remaining.Any(a =>
                    a.Status == AlertStatus.NOT_RESOLVED &&
                    (a.Type == AlertType.CASH_BOX_FULL ||
                     a.Type == AlertType.PAPER_EMPTY ||
                     a.Type == AlertType.HARDWARE_FAILURE));


                var parkmeter = await parkmeterRepo.GetByIdAsync(parkmeterID);


                if (!stillBlocked && parkmeter != null &&
                    parkmeter.Status == MeterStatus.TEMPORARILY_UNAVAILABLE)
                {
                    parkmeter.Status = MeterStatus.IN_SERVICE;
                    await parkmeterRepo.UpdateAsync(parkmeter);
                    await publisher.PublishStatusAsync(
                        parkmeterID,
                        MeterStatus.IN_SERVICE.ToString());

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"✅ {parkmeter.SerialNumber} → IN_SERVICE (door closed)");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"DoorClosed error: {ex.Message}");
                Console.ResetColor();
            }
        }

        // parkmeter connected again after lost connection
        private async Task HandleReconnectedAsync(string payload)
        {
            try
            {
                var raw = JsonSerializer.Deserialize<JsonElement>(payload);
                var parkmeterID = Guid.Parse(
                    raw.GetProperty("ParkmeterId").GetString()!);

                using var scope = _serviceProvider.CreateScope();
                var alertRepo = scope.ServiceProvider
                    .GetRequiredService<IAlertRepository>();

                // Auto-resolve CONNECTION_LOST alerts
                var alerts = await alertRepo.GetByParkmeterAsync(parkmeterID);
                foreach (var a in alerts.Where(a =>
                    a.Status == AlertStatus.NOT_RESOLVED &&
                    a.Type == AlertType.CONNECTION_LOST))
                {
                    a.Status = AlertStatus.RESOLVED;
                    a.ResolvedAt = DateTime.UtcNow;
                    await alertRepo.UpdateAsync(a);
                    Console.WriteLine($"✅ Auto-resolved on reconnect: CONNECTION_LOST");
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Reconnected error: {ex.Message}");
                Console.ResetColor();
            }
        }
        
        public async Task StopAsync()
        {
            if (_client != null)
                await _client.DisconnectAsync();
        }
    }

}