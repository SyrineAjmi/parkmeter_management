using MQTTnet;
using MQTTnet.Client;
using parkmeter_management.Domain.Enums;
using System.Text.Json;

namespace parkmeter_management.SimulatorCore
{
    public class ParkMeterSimulator
    {
        // constants
        private const int CASH_WARN_THRESHOLD = 80;
        private const int CASH_FULL_THRESHOLD = 100;
        private const int PAPER_LOW_THRESHOLD = 20;
        private const int PAPER_EMPTY_THRESHOLD = 5;
        private const int HARDWARE_FAILURE_CHANCE = 500;

        private readonly ParkMeterState _state;
        private readonly Random _random = new();
        private IMqttClient? _client;

        public ParkMeterSimulator(ParkMeterState state)
        {
            _state = state;
        }

        // public properties
        public bool HasConfig => _state.HasConfig;
        public string Status => _state.Status;
        public int CashLevel => _state.CashLevel;
        public int PaperLevel => _state.PaperLevel;
        public bool HardwareFailure => _state.HardwareFailure;
        public bool DoorOpen => _state.DoorOpen;
        public bool IsOffline => _state.IsOffline;
        public bool IsConnectionLost => _state.IsConnectionLost;
        public bool CashBoxWarnSent => _state.CashBoxWarnSent;
        public bool CashBoxAlertSent => _state.CashBoxAlertSent;
        public bool PaperAlertSent => _state.PaperAlertSent;
        public bool PaperEmptyAlertSent => _state.PaperEmptyAlertSent;
        public string SerialNumber => _state.SerialNumber;

        //----------------------------------------------------------
        // lifecycle

        public async Task RunAsync(CancellationToken ct = default)
        {
            while (!ct.IsCancellationRequested)
            {
                _client = new MqttFactory().CreateMqttClient();

                if (!await ConnectWithRetryAsync(ct)) return;

                RegisterMessageListener();

                // subscribe to backend commands
                await _client.SubscribeAsync(
                    $"parkmeters/{_state.Id}/commands/config",
                    cancellationToken: ct);
                await _client.SubscribeAsync(
                    $"parkmeters/{_state.Id}/commands/status",
                    cancellationToken: ct);

                if (_state.WasConnectionLost)
                {
                    _state.WasConnectionLost = false;
                    await PublishAsync(
                    $"parkmeters/{_state.Id}/reconnected",
                    new { ParkmeterId = _state.Id.ToString(), CreatedAt = DateTime.UtcNow });

                    await PublishEventAsync(ParkMeterEventType.MACHINE_RECONNECTED);
                    Log("Reconnected → notified backend", ConsoleColor.Green);
                }
                else
                {

                    await PublishAsync($"parkmeters/{_state.Id}/online",new { ParkmeterId = _state.Id.ToString(), CreatedAt = DateTime.UtcNow });

                    await PublishEventAsync(ParkMeterEventType.MACHINE_ONLINE);
                    Log("Online → notified backend", ConsoleColor.Green);
                }

                Log($"Simulation started | " +
                    $"Price: {_state.PricePerHour} DT/h | " +
                    $"Max: {_state.MaxDuration} min",
                    ConsoleColor.White);

                await RunSimulationLoopAsync(ct);

                if (_state.IsOffline && !_state.IsConnectionLost)
                {
                    Log("Power failure — parkmeter stopped", ConsoleColor.Red);
                    return;
                }

                if (_state.IsConnectionLost)
                {
                    Log("Connection lost — attempting reconnect...",
                        ConsoleColor.Yellow);
                    _state.IsConnectionLost = false;
                    _state.IsOffline = false;
                    await Task.Delay(10000, ct);
                }
            }
        }

        public async Task StopAsync()
        {
            if (_client != null && _client.IsConnected)
            {
                await PublishEventAsync(ParkMeterEventType.MACHINE_OFFLINE);
                await _client.DisconnectAsync();
                Log("Powered OFF", ConsoleColor.Red);
            }
        }

        // test mode — force any alert type
        public async Task TriggerAlertAsync(AlertType alertType)
        {
            if (alertType.HasFlag(AlertType.CASH_BOX_FULL))
                _state.CashLevel = 100;
            if (alertType.HasFlag(AlertType.CASH_BOX_WARN))
                _state.CashLevel = 65;
            if (alertType.HasFlag(AlertType.PAPER_EMPTY))
                _state.PaperLevel = 0;
            if (alertType.HasFlag(AlertType.PAPER_LOW))
                _state.PaperLevel = 15;

            await SendAlertAndUpdateStateAsync(alertType);
            Log($"[TEST] Triggered: {alertType} ({(int)alertType})",
                ConsoleColor.Magenta);
        }

        //----------------------------------------------------------
        // operator actions — published as events to /events topic

        public async Task EmptyCashBoxAsync()
        {
            _state.CashLevel = 0;
            _state.CashBoxWarnSent = false;
            _state.CashBoxAlertSent = false;
            _state.DoorOpen = false;
            _state.DoorOpenAlertSent = false;

            await PublishEventAsync(ParkMeterEventType.CASH_COLLECTED);
            Log("Cash collected → events/CASH_COLLECTED ✅", ConsoleColor.Green);
        }

        public async Task RefillPaperAsync()
        {
            _state.PaperLevel = 100;
            _state.PaperAlertSent = false;
            _state.PaperEmptyAlertSent = false;
            _state.DoorOpen = false;
            _state.DoorOpenAlertSent = false;

            await PublishEventAsync(ParkMeterEventType.PAPER_REFILLED);
            Log("Paper refilled → events/PAPER_REFILLED ✅", ConsoleColor.Green);
        }

        public async Task CloseDoorAsync()
        {
            _state.DoorOpen = false;
            _state.DoorOpenAlertSent = false;

            await PublishEventAsync(ParkMeterEventType.DOOR_CLOSED);
            Log("Door closed → events/DOOR_CLOSED ✅", ConsoleColor.Green);
        }

        //----------------------------------------------------------
        // connection

        private async Task<bool> ConnectWithRetryAsync(CancellationToken ct)
        {
            var options = new MqttClientOptionsBuilder()
                .WithTcpServer("localhost", 1883)
                .WithClientId($"simulator-{_state.SerialNumber}")
                .Build();

            for (int attempt = 1; attempt <= 5; attempt++)
            {
                try
                {
                    await _client!.ConnectAsync(options, ct);
                    Log("Connected to broker", ConsoleColor.Green);
                    return true;
                }
                catch
                {
                    Log($"Connection attempt {attempt}/5...",
                        ConsoleColor.Yellow);
                    await Task.Delay(2000, ct);
                }
            }

            Log("Could not connect after 5 attempts.", ConsoleColor.Red);
            return false;
        }

        private void RegisterMessageListener()
        {
            _client!.ApplicationMessageReceivedAsync += e =>
            {
                var topic = e.ApplicationMessage.Topic;
                var payload = e.ApplicationMessage.ConvertPayloadToString();

                try
                {
                    // backend → device commands
                    if (topic.EndsWith("/commands/config"))
                        ApplyConfig(payload);
                    else if (topic.EndsWith("/commands/status"))
                        ApplyStatus(payload);
                }
                catch (Exception ex)
                {
                    Log($"Message error: {ex.Message}", ConsoleColor.Red);
                }

                return Task.CompletedTask;
            };
        }

        private void ApplyConfig(string payload)
        {
            var raw = JsonSerializer.Deserialize<JsonElement>(payload);
            var newPrice = raw.GetProperty("PricePerHour").GetDecimal();
            var newMax = raw.GetProperty("MaxDuration").GetInt32();

            Log($"Config received: " +
                $"{_state.PricePerHour} → {newPrice} DT/h | " +
                $"{_state.MaxDuration} → {newMax} min",
                ConsoleColor.Cyan);

            _state.PricePerHour = newPrice;
            _state.MaxDuration = newMax;
            _state.HasConfig = true;
        }

        private void ApplyStatus(string payload)
        {
            var newStatus = payload.Trim();
            if (newStatus == _state.Status) return;

            var old = _state.Status;
            _state.Status = newStatus;

            if (newStatus == "IN_SERVICE")
            {
                _state.IsOffline = false;
                _state.HardwareFailure = false;
                _state.DoorOpen = false;
                _state.DoorOpenAlertSent = false;

                if (_state.CashBoxAlertSent)
                {
                    _state.CashLevel = 0;
                    _state.CashBoxWarnSent = false;
                    _state.CashBoxAlertSent = false;
                }

                if (_state.PaperEmptyAlertSent)
                {
                    _state.PaperLevel = 100;
                    _state.PaperAlertSent = false;
                    _state.PaperEmptyAlertSent = false;
                }
            }

            var color = newStatus switch
            {
                "IN_SERVICE" => ConsoleColor.Green,
                "TEMPORARILY_UNAVAILABLE" => ConsoleColor.Yellow,
                "OUT_OF_SERVICE" => ConsoleColor.Red,
                "MAINTENANCE" => ConsoleColor.DarkYellow,
                _ => ConsoleColor.White
            };

            Log($"Status: {old} → {newStatus}", color);
        }

        //----------------------------------------------------------
        // simulation loop

        private async Task RunSimulationLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    if (_state.IsOffline)
                    {
                        Log("Disconnecting from broker...", ConsoleColor.Red);
                        await StopAsync();
                        return;
                    }

                    if (!_state.HasConfig)
                        Log("No configuration — waiting for config push...",
                            ConsoleColor.Yellow);
                    else
                        await HandleCurrentStatusAsync();

                    await CheckAndSendAlertsAsync();

                    await Task.Delay(
                        _random.Next(
                            _state.MinTickDelayMs,
                            _state.MaxTickDelayMs), ct);
                }
                catch (OperationCanceledException) { break; }
            }
        }

        private async Task HandleCurrentStatusAsync()
        {
            switch (_state.Status)
            {
                case "IN_SERVICE":
                    await HandleInServiceAsync();
                    break;
                case "TEMPORARILY_UNAVAILABLE":
                    Log("Temporarily unavailable...", ConsoleColor.Yellow);
                    break;
                case "MAINTENANCE":
                    Log("Under maintenance...", ConsoleColor.DarkYellow);
                    break;
                default:
                    Log($"Status: {_state.Status} — waiting for activation...",
                        ConsoleColor.Yellow);
                    break;
            }
        }

        private async Task HandleInServiceAsync()
        {
            if (_state.CashLevel >= CASH_FULL_THRESHOLD)
            { Log("Cash box full — waiting for emptying...", ConsoleColor.Red); return; }

            if (_state.PaperLevel <= 0)
            { Log("No paper — waiting for refill...", ConsoleColor.Red); return; }

            if (_state.HardwareFailure)
            { Log("Hardware failure — waiting for fix...", ConsoleColor.Red); return; }

            if (_state.DoorOpen)
            { Log("Door open — waiting for close...", ConsoleColor.Yellow); return; }

            var (amount, duration, status) = await SendTransactionAsync();
            UpdateState(status);
            LogTransaction(amount, duration, status);
        }

        //----------------------------------------------------------
        // transactions

        private async Task<(decimal amount, int duration, TransactionStatus status)>
            SendTransactionAsync()
        {
            var duration = _random.Next(15, _state.MaxDuration);
            var amount = Math.Round(
                _state.PricePerHour * duration / 60, 2);
            var status = GetTransactionStatus();

            // transactions still go on their own topic
            await PublishAsync(
                $"parkmeters/{_state.Id}/transactions",
                new
                {
                    ParkmeterId = _state.Id.ToString(),
                    Amount = amount,
                    Duration = duration,
                    Status = status.ToString(),
                    CreatedAt = DateTime.UtcNow
                });

            await PublishEventAsync(ParkMeterEventType.TRANSACTION_DONE);

            return (amount, duration, status);
        }

        private TransactionStatus GetTransactionStatus()
        {
            var chance = _random.Next(100);
            return chance switch
            {
                < 85 => TransactionStatus.COMPLETED,
                < 90 => TransactionStatus.FAILED,
                < 95 => TransactionStatus.CANCELLED,
                _ => TransactionStatus.EXPIRED
            };
        }

        private void LogTransaction(
            decimal amount, int duration, TransactionStatus status)
        {
            var color = status == TransactionStatus.COMPLETED
                ? ConsoleColor.Green
                : ConsoleColor.Yellow;

            Log($"Transaction: {amount} DT / {duration} min | " +
                $"Status: {status} | " +
                $"Cash: {Math.Min(_state.CashLevel, 100)}% | " +
                $"Paper: {Math.Max(_state.PaperLevel, 0)}%",
                color);
        }

        private void UpdateState(TransactionStatus status)
        {
            _state.TransactionCount++;

            if (status == TransactionStatus.COMPLETED)
            {
                _state.CashLevel += _random.Next(
                    _state.CashFillPerTick - 2,
                    _state.CashFillPerTick + 5);
                _state.PaperLevel -= _random.Next(
                    _state.PaperUsePerTick - 1,
                    _state.PaperUsePerTick + 2);
            }
        }

        //----------------------------------------------------------
        // alert checking

        private async Task CheckAndSendAlertsAsync()
        {
            await CheckCashBoxAsync();
            await CheckPaperAsync();
            await CheckRandomHardwareAsync();
        }

        private async Task CheckCashBoxAsync()
        {
            if (_state.CashLevel >= CASH_WARN_THRESHOLD
                && !_state.CashBoxWarnSent)
                await SendAlertAndUpdateStateAsync(AlertType.CASH_BOX_WARN);

            if (_state.CashLevel >= CASH_FULL_THRESHOLD
                && !_state.CashBoxAlertSent)
                await SendAlertAndUpdateStateAsync(AlertType.CASH_BOX_FULL);
        }

        private async Task CheckPaperAsync()
        {
            if (_state.PaperLevel <= PAPER_LOW_THRESHOLD
                && !_state.PaperAlertSent)
                await SendAlertAndUpdateStateAsync(AlertType.PAPER_LOW);

            if (_state.PaperLevel <= PAPER_EMPTY_THRESHOLD
                && !_state.PaperEmptyAlertSent)
                await SendAlertAndUpdateStateAsync(AlertType.PAPER_EMPTY);
        }

        private async Task CheckRandomHardwareAsync()
        {
            if (_random.Next(HARDWARE_FAILURE_CHANCE) == 0)
                await SendAlertAndUpdateStateAsync(AlertType.HARDWARE_FAILURE);
        }

        private async Task SendAlertAndUpdateStateAsync(AlertType alertType)
        {
            await SendAlertAsync(alertType);
            UpdateLocalAlertState(alertType);
        }

        private void UpdateLocalAlertState(AlertType alertType)
        {
            if (alertType.HasFlag(AlertType.CASH_BOX_WARN))
            {
                _state.CashBoxWarnSent = true;
                Log($"CASH_BOX_WARN ({_state.CashLevel}%)", ConsoleColor.Yellow);
            }
            if (alertType.HasFlag(AlertType.CASH_BOX_FULL))
            {
                _state.CashLevel = 100;
                _state.CashBoxWarnSent = true;
                _state.CashBoxAlertSent = true;
                Log("CASH_BOX_FULL — transactions stopped", ConsoleColor.Red);
            }
            if (alertType.HasFlag(AlertType.PAPER_LOW))
            {
                _state.PaperAlertSent = true;
                Log($"PAPER_LOW ({_state.PaperLevel}%)", ConsoleColor.Yellow);
            }
            if (alertType.HasFlag(AlertType.PAPER_EMPTY))
            {
                _state.PaperEmptyAlertSent = true;
                Log("PAPER_EMPTY — transactions stopped", ConsoleColor.Red);
            }
            if (alertType.HasFlag(AlertType.HARDWARE_FAILURE))
            {
                _state.HardwareFailure = true;
                Log("HARDWARE_FAILURE — transactions stopped", ConsoleColor.Red);
            }
            if (alertType.HasFlag(AlertType.DOOR_OPEN))
            {
                _state.DoorOpen = true;
                _state.DoorOpenAlertSent = true;
                Log("DOOR_OPEN — transactions stopped", ConsoleColor.Yellow);
            }
            if (alertType.HasFlag(AlertType.POWER_FAILURE))
            {
                _state.IsOffline = true;
                _state.IsConnectionLost = false;
                Log("POWER_FAILURE — going offline...", ConsoleColor.Red);
            }
            if (alertType.HasFlag(AlertType.CONNECTION_LOST))
            {
                _state.IsOffline = true;
                _state.IsConnectionLost = true;
                _state.WasConnectionLost = true;
                Log("CONNECTION_LOST — will reconnect...", ConsoleColor.Yellow);
            }
        }

        //----------------------------------------------------------
        // MQTT publishing

        /// <summary>
        /// Publishes a machine alert to parkmeters/{id}/alerts
        /// </summary>
        private async Task SendAlertAsync(AlertType alertType)
        {
            await PublishAsync(
                $"parkmeters/{_state.Id}/alerts",
                new
                {
                    ParkmeterId = _state.Id.ToString(),
                    AlertType = (int)alertType,
                    CreatedAt = DateTime.UtcNow
                });
        }

        /// <summary>
        /// Publishes an operator/lifecycle event to parkmeters/{id}/events
        /// </summary>
        private async Task PublishEventAsync(
            ParkMeterEventType eventType,
            object? extraPayload = null)
        {
            await PublishAsync(
                $"parkmeters/{_state.Id}/events",
                new
                {
                    ParkmeterId = _state.Id.ToString(),
                    EventType = (int)eventType,
                    Data = extraPayload,
                    CreatedAt = DateTime.UtcNow
                });
        }

        private async Task PublishAsync(string topic, object payload)
        {
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(JsonSerializer.Serialize(payload))
                .WithQualityOfServiceLevel(
                    MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            await _client!.PublishAsync(message);
        }

        //----------------------------------------------------------
        // logging

        private void Log(string message, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine($"[{_state.SerialNumber}] {message}");
            Console.ResetColor();
        }
    }
}