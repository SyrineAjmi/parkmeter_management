using MQTTnet;
using MQTTnet.Client;
using parkmeter_management.Domain.Enums;
using System.Text.Json;

namespace parkmeter_management.SimulatorCore 
{
    public class ParkMeterSimulator // behaviour of a parkmeter

    {
        // constants
        private const int CASH_WARN_THRESHOLD = 80;
        private const int CASH_FULL_THRESHOLD = 100;
        private const int PAPER_LOW_THRESHOLD = 20;
        private const int PAPER_EMPTY_THRESHOLD = 5;
        private const int HARDWARE_FAILURE_CHANCE = 500;


        private readonly ParkMeterState _state;
        private readonly Random _random = new(); // for random
        private IMqttClient? _client;

        public ParkMeterSimulator(ParkMeterState state) // state shared across all methods
        {
            _state = state;
        }


        //----------------------------------------------------------

        // public properties for simulator
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
        public string StrategyType => _state.StrategyType;


        //----------------------------------------------------------

        // lifecycle
        // start
        public async Task RunAsync(CancellationToken ct = default)
        {
            while (!ct.IsCancellationRequested)
            {
                _client = new MqttFactory().CreateMqttClient();

                if (!await ConnectWithRetryAsync(ct)) return;

                RegisterMessageListener(ct);

                await _client.SubscribeAsync(
                    $"parkmeters/{_state.Id}/config",
                    cancellationToken: ct);

                await _client.SubscribeAsync(
                    $"parkmeters/{_state.Id}/status",
                    cancellationToken: ct);

                // publish reconnected if CONNECTION_LOST
                if (_state.WasConnectionLost)
                {
                    await PublishEventAsync(MachineEventType.MACHINE_RECONNECTED);
                    _state.WasConnectionLost = false; 
                    Log("Reconnected → notified backend",
                        ConsoleColor.Green);
                }
                else
                {
                    // publish online on first start
                    await PublishEventAsync(MachineEventType.MACHINE_ONLINE);

                    Log("Online → notified backend",
                        ConsoleColor.Green);
                }

                await RunSimulationLoopAsync(ct);

                if (_state.IsOffline && !_state.IsConnectionLost)
                {
                    Log("Power failure parkmeter stopped",
                        ConsoleColor.Red);
                    return;
                }

                if (_state.IsConnectionLost)
                {
                    Log("Connection lost attempting reconnect...",
                        ConsoleColor.Yellow);
                    _state.IsConnectionLost = false;
                    _state.IsOffline = false;
                    await Task.Delay(10000, ct);
                }
            }
        }

        //----------------------------------------------------------

        // stop
        public async Task StopAsync()
        {
            if (_client != null && _client.IsConnected)
            {
                await _client.DisconnectAsync();
                Log("Powered OFF", ConsoleColor.Red);
            }
        }

        //----------------------------------------------------------

        // test mode
        public async Task TriggerAlertAsync(AlertType alertType)
        {
            await SendAlertAndUpdateStateAsync(alertType);
            Log($"[TEST] Triggered: {alertType} ({(int)alertType})",
                ConsoleColor.Magenta);
        }

        //----------------------------------------------------------

        //   operator actions 
        public async Task EmptyCashBoxAsync()
        {
            _state.CashLevel = 0;
            _state.CashBoxWarnSent = false;
            _state.CashBoxAlertSent = false;
            _state.DoorOpen = false;

            // specific topic 
            await PublishEventAsync(MachineEventType.CASH_COLLECTED);

            Log("Cash box emptied → transactions will resume",
                ConsoleColor.Green);
        }

        public async Task RefillPaperAsync()
        {

            _state.PaperLevel = 100;
            _state.PaperAlertSent = false;
            _state.PaperEmptyAlertSent = false;
            _state.DoorOpen = false;

            // specofic topic
            await PublishEventAsync(MachineEventType.PAPER_REFILLED);

            Log("Paper refilled : transactions will resume",
                ConsoleColor.Green);
        }

        public async Task CloseDoorAsync() 
        {
            _state.DoorOpen = false;

            // notify backend door is close
            await PublishEventAsync(MachineEventType.DOOR_CLOSED);


            Log("Door closed..",
                ConsoleColor.Green);
        }

        //----------------------------------------------------------

        // connection 

        // resilient connection 
        private async Task<bool> ConnectWithRetryAsync(CancellationToken ct)
        {
            var options = new MqttClientOptionsBuilder()
                .WithTcpServer("localhost", 1883)
                .WithClientId($"simulator-{_state.SerialNumber}")
                .Build();

            for (int attempt = 1; attempt <= 5; attempt++) // tries multiple times before giving up
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

        // listen for backend messages 
        private void RegisterMessageListener(CancellationToken ct)
        {
            _client!.DisconnectedAsync += async e =>
            {
                if (ct.IsCancellationRequested) return;
                if (_state.IsOffline && !_state.IsConnectionLost) return;

                Log("Unexpected disconnection — will reconnect...",
                    ConsoleColor.Yellow);

                _state.IsConnectionLost = true;
                _state.IsOffline = true;
                _state.WasConnectionLost = true;
            };

            _client!.ApplicationMessageReceivedAsync += e => 
            {
                var topic = e.ApplicationMessage.Topic;
                var payload = e.ApplicationMessage.ConvertPayloadToString();

                try
                {
                    if (topic.EndsWith("/config"))
                        ApplyConfig(payload);
                    else if (topic.EndsWith("/status"))
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

            _state.MaxDuration = raw
                .GetProperty("maxDuration").GetInt32();

            var pricing = raw.GetProperty("pricing");
            _state.StrategyType = pricing
                .GetProperty("strategy").GetString()!;

            var parameters = pricing.GetProperty("parameters");

            // reset rates
            _state.PricePerHour = 0;
            _state.PeakRate = 0;
            _state.OffPeakRate = 0;
            _state.WeekdayRate = 0;
            _state.WeekendRate = 0;

            switch (_state.StrategyType)
            {
                case "FLAT_RATE":
                    _state.PricePerHour = parameters
                        .GetProperty("pricePerHour").GetDecimal();
                    break;

                case "TIME_OF_DAY":
                    _state.PeakRate = parameters
                        .GetProperty("peakRate").GetDecimal();
                    _state.OffPeakRate = parameters
                        .GetProperty("offPeakRate").GetDecimal();
                    break;

                case "WEEKDAY_WEEKEND":
                    _state.WeekdayRate = parameters
                        .GetProperty("weekdayRate").GetDecimal();
                    _state.WeekendRate = parameters
                        .GetProperty("weekendRate").GetDecimal();
                    break;
            }

            _state.HasConfig = true;

            Log($"Config received: Strategy={_state.StrategyType} | " +
                $"MaxDuration={_state.MaxDuration} min",
                ConsoleColor.Cyan);
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
                _state.PowerFailureAlertSent = false; 

                // reset cash if was blocked
                if (_state.CashBoxAlertSent)
                {
                    _state.CashLevel = 0;
                    _state.CashBoxWarnSent = false;
                    _state.CashBoxAlertSent = false;
                }

                // reset paper if was blocked
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

            Log($"Status: {old} to {newStatus}", color);
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
                    {
                        Log("No configuration — waiting for config push...",
                            ConsoleColor.Yellow);
                    }
                    else
                    {
                        await HandleCurrentStatusAsync();
                        await CheckAndSendAlertsAsync();
                    }

                    await Task.Delay(
                        _random.Next(
                            _state.MinTickDelayMs,
                            _state.MaxTickDelayMs), ct);
                }
                catch (OperationCanceledException)
                {
                    await StopAsync();
                    return;
                }
            }
        }

        // routing by status  
        private async Task HandleCurrentStatusAsync()
        {
            switch (_state.Status)
            {
                case "IN_SERVICE":
                    await HandleInServiceAsync();
                    break;
                case "TEMPORARILY_UNAVAILABLE":
                    Log("Temporarily unavailable...",
                        ConsoleColor.Yellow);
                    break;
                case "MAINTENANCE":
                    Log("Under maintenance...",
                        ConsoleColor.DarkYellow);
                    break;
                default:
                    Log($"Status: {_state.Status} — waiting for activation...",
                        ConsoleColor.Yellow);
                    break;
            }

        }

        //handling normal operation
        private async Task HandleInServiceAsync()
        {
            // Check blocking conditions
            if (_state.CashLevel >= CASH_FULL_THRESHOLD)
            {
                Log("Cash box full — waiting for emptying...",
                    ConsoleColor.Red);
                return;
            }

            if (_state.PaperLevel <= 0)
            {
                Log("No paper — waiting for refill...",
                    ConsoleColor.Red);
                return;
            }

            if (_state.HardwareFailure)
            {
                Log("Hardware failure — waiting for fix...",
                    ConsoleColor.Red);
                return;
            }

            if (_state.DoorOpen)
            {
                Log("Door open — waiting for close...",
                    ConsoleColor.Yellow);
                return;
            }

            // send transaction
            var (amount, duration, status) =
                await SendTransactionAsync();
            UpdateState(status);
            LogTransaction(amount, duration, status);
        }

        //----------------------------------------------------------

        //  transactions 

        private async Task<(decimal amount, int duration, TransactionStatus status)>
        SendTransactionAsync()
        {
            var duration = _random.Next(15, _state.MaxDuration);
            var now = DateTime.UtcNow;
            var status = GetTransactionStatus();

            var amount = status == TransactionStatus.COMPLETED
                ? CalculateAmount(duration, now)
                : 0;

            await PublishAsync(
                $"parkmeters/{_state.Id}/transactions",
                new
                {
                    ParkmeterId = _state.Id.ToString(),
                    Amount = amount,
                    Duration = duration,
                    Status = status.ToString(),
                    CreatedAt = now
                });

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
                $"Status: {status} | " + $"Cash: {_state.CashLevel}% | Paper: {_state.PaperLevel}%",
                color);
        }

        private void UpdateState(TransactionStatus status)
        {
            _state.TransactionCount++;

            if (status == TransactionStatus.COMPLETED)
            {
                _state.CashLevel = Math.Min(100,
                    _state.CashLevel + _random.Next(
                        _state.CashFillPerTick - 2,
                        _state.CashFillPerTick + 5));

                _state.PaperLevel = Math.Max(0,
                    _state.PaperLevel - _random.Next(
                        _state.PaperUsePerTick - 1,
                        _state.PaperUsePerTick + 2));
            }
        }

        //----------------------------------------------------------

        // alert checking

        // every time calls in order on to see if there are problems
        private async Task CheckAndSendAlertsAsync()
        {
            await CheckCashBoxAsync();
            await CheckPaperAsync();
            await CheckRandomHardwareAsync();
        }
         

        // monitoring cash levels
        private async Task CheckCashBoxAsync()
        {
            if (_state.CashLevel >= CASH_WARN_THRESHOLD
                && !_state.CashBoxWarnSent) //prevent duplic
                await SendAlertAndUpdateStateAsync(AlertType.CASH_BOX_WARN);

            if (_state.CashLevel >= CASH_FULL_THRESHOLD
                && !_state.CashBoxAlertSent)//prevent duplic
                await SendAlertAndUpdateStateAsync(AlertType.CASH_BOX_FULL);
        }


        //monitoring paper levels
        private async Task CheckPaperAsync()
        {
            if (_state.PaperLevel <= PAPER_LOW_THRESHOLD
                && !_state.PaperAlertSent)
                await SendAlertAndUpdateStateAsync(AlertType.PAPER_LOW);

            if (_state.PaperLevel <= PAPER_EMPTY_THRESHOLD
                && !_state.PaperEmptyAlertSent)
                await SendAlertAndUpdateStateAsync(AlertType.PAPER_EMPTY);
        }


        // random failures (avg 1 hardw failure every 2H)
        private async Task CheckRandomHardwareAsync()
        {
            {
                if (!_state.HardwareFailure &&
                    _random.Next(HARDWARE_FAILURE_CHANCE) == 0)
                    await SendAlertAndUpdateStateAsync(AlertType.HARDWARE_FAILURE);
            }
        }


        // core alert logic
        private async Task SendAlertAndUpdateStateAsync(AlertType alertType)
        {
            await SendAlertAsync(alertType);// send alert to backend
            UpdateLocalState(alertType); // update local state
        }

        private void UpdateLocalState(AlertType alertType) 
        {

            if (alertType.HasFlag(AlertType.CASH_BOX_WARN))
            {
                _state.CashLevel = Math.Max(_state.CashLevel, 65); 
                _state.CashBoxWarnSent = true;
                Log($"CASH_BOX_WARN ({_state.CashLevel}%)", ConsoleColor.Yellow);
            }

            if (alertType.HasFlag(AlertType.CASH_BOX_FULL))
            {
                _state.CashLevel = 100;
                _state.CashBoxWarnSent = true;
                _state.CashBoxAlertSent = true;
                Log($"CASH_BOX_FULL ({_state.CashLevel}%)", ConsoleColor.Red);
            }

            if (alertType.HasFlag(AlertType.PAPER_LOW))
            {
                _state.PaperLevel = Math.Min(_state.PaperLevel, 15); 
                _state.PaperAlertSent = true;
                Log($"PAPER_LOW ({_state.PaperLevel}%)", ConsoleColor.Yellow);
            }

            if (alertType.HasFlag(AlertType.PAPER_EMPTY))
            {
                _state.PaperLevel = 0;
                _state.PaperEmptyAlertSent = true;
                Log("PAPER_EMPTY : transactions stopped", ConsoleColor.Red);
            }

            // hardware
            if (alertType.HasFlag(AlertType.HARDWARE_FAILURE))
            {
                _state.HardwareFailure = true;
                Log("HARDWARE_FAILURE : transactions stopped",
                    ConsoleColor.Red);
            }

            if (alertType.HasFlag(AlertType.DOOR_OPEN))
            {
                _state.DoorOpen = true;
                Log("DOOR_OPEN : transactions stopped",
                    ConsoleColor.Yellow);
            }

            if (alertType.HasFlag(AlertType.POWER_FAILURE))
            {
                if (_state.PowerFailureAlertSent) return; 
                _state.PowerFailureAlertSent = true;
                _state.IsOffline = true;
                _state.IsConnectionLost = false;
                Log("POWER_FAILURE : going offline...", ConsoleColor.Red);
            }

            if (alertType.HasFlag(AlertType.CONNECTION_LOST))
            {
                _state.IsOffline = true;
                _state.IsConnectionLost = true;
                _state.WasConnectionLost = true; 
                Log("CONNECTION_LOST : disconnecting, will reconnect...",
                    ConsoleColor.Yellow);
            }
        }


        private async Task PublishEventAsync(MachineEventType eventType)
        {
            var envelope = new
            {
                ParkmeterId = _state.Id.ToString(),
                EventType = (int)eventType,
                CreatedAt = DateTime.UtcNow.ToString("O")
            };

            await PublishAsync(
                $"parkmeters/{_state.Id}/events", envelope);
        }


        // helper for calcul amount 
        private decimal CalculateAmount(int durationMinutes, DateTime time)
        {
            return _state.StrategyType switch
            {
                "TIME_OF_DAY" =>
                    time.Hour >= 8 && time.Hour < 18
                        ? Math.Round(_state.PeakRate * durationMinutes / 60, 3)
                        : Math.Round(_state.OffPeakRate * durationMinutes / 60, 3),

                "WEEKDAY_WEEKEND" =>
                    time.DayOfWeek == DayOfWeek.Saturday ||
                    time.DayOfWeek == DayOfWeek.Sunday
                        ? Math.Round(_state.WeekendRate * durationMinutes / 60, 3)
                        : Math.Round(_state.WeekdayRate * durationMinutes / 60, 3),

                _ => Math.Round(_state.PricePerHour * durationMinutes / 60, 3)
            };
        }

        //----------------------------------------------------------

        //  MQTT 
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

        private async Task PublishAsync(string topic, object payload)
        {
            if (_client?.IsConnected != true)
            {
                Log($"[WARN] Cannot publish — not connected",
                    ConsoleColor.Yellow);
                return;
            }

            try
            {
                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(JsonSerializer.Serialize(payload))
                    .WithQualityOfServiceLevel(
                        MQTTnet.Protocol.MqttQualityOfServiceLevel
                        .AtLeastOnce)
                    .Build();

                await _client!.PublishAsync(message);
            }
            catch (Exception ex)
            {
                Log($"[WARN] Publish failed: {ex.Message}",
                    ConsoleColor.Yellow);

                // trigger reconnection
                _state.IsConnectionLost = true;
                _state.IsOffline = true;
                _state.WasConnectionLost = true;
            }
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

