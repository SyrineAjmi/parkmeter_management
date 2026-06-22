using MQTTnet;
using MQTTnet.Client;
using parkmeter_management.Domain.Enums;
using System.Text.Json;

namespace parkmeter_management.SimulatorCore
{
    public class ParkMeterSimulator
    {
        // constants
        private const int PAPER_LOW_THRESHOLD = 20;
        private const int PAPER_EMPTY_THRESHOLD = 5;
        private const int HARDWARE_FAILURE_CHANCE = 500;

        private readonly ParkMeterState _state;
        private readonly Random _random = new();
        private IMqttClient? _client;

        private readonly Action<string, string>? _logCallback;

        public ParkMeterSimulator(ParkMeterState state)
        {
            _state = state;
        }

        // public properties 

        public bool HasConfig => _state.HasConfig;
        public string Status => _state.Status;
        public decimal CashAmount => _state.CashAmount; 
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
        public int TransactionCount => _state.TransactionCount;

        //  lifecycle 

        public async Task RunAsync(CancellationToken ct = default)
        {
            _client = new MqttFactory().CreateMqttClient(); 
            RegisterMessageListener(ct); 

            while (!ct.IsCancellationRequested)
            {
                if (!await ConnectWithRetryAsync(ct)) return;

                // subscribe each reconnect
                await _client.SubscribeAsync(
                    $"parkmeters/{_state.Id}/config",
                    cancellationToken: ct);
                await _client.SubscribeAsync(
                    $"parkmeters/{_state.Id}/command",
                    cancellationToken: ct);

                if (_state.WasConnectionLost)
                {
                    await PublishEventAsync(MachineEventType.MACHINE_RECONNECTED);
                    _state.WasConnectionLost = false;
                    await PublishStatusAsync(_state.Status);
                    Log("Reconnected → notified backend", ConsoleColor.Green);
                }
                else
                {
                    await PublishEventAsync(MachineEventType.MACHINE_ONLINE);
                    Log("Online → notified backend", ConsoleColor.Green);
                }

                await RunSimulationLoopAsync(ct);

                if (_state.IsOffline && !_state.IsConnectionLost)
                {
                    Log("Power failure — stopped", ConsoleColor.Red);
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

        // stop

        public async Task StopAsync()
        {
            if (_client != null && _client.IsConnected)
            {
                _state.IsIntentionalDisconnect = true; 
                await _client.DisconnectAsync();
                Log("Powered OFF", ConsoleColor.Red);
            }
        }

        //  test mode 

        public async Task TriggerAlertAsync(AlertType alertType)
        {
            if (alertType.HasFlag(AlertType.CASH_BOX_WARN))
                _state.CashAmount = _state.CashWarnThreshold;

            if (alertType.HasFlag(AlertType.CASH_BOX_FULL))
                _state.CashAmount = _state.CashFullThreshold;

            if (alertType.HasFlag(AlertType.PAPER_LOW))
                _state.PaperLevel = 15;

            if (alertType.HasFlag(AlertType.PAPER_EMPTY))
                _state.PaperLevel = 0;

            await SendAlertAndUpdateStateAsync(alertType);
            await PublishTelemetryAsync();
        }

        // agent actions 

        public async Task EmptyCashBoxAsync()
        {
            _state.CashAmount = 0;
            _state.CashBoxWarnSent = false;
            _state.CashBoxAlertSent = false;
            _state.TransactionCount = 0;
            SaveState(); 


            await PublishEventAsync(MachineEventType.CASH_COLLECTED);
            await PublishTelemetryAsync();

            // machine reports its status

            if (CanBeInService() && _state.Status != "IN_SERVICE" )
            {
                _state.Status = "IN_SERVICE";
                await PublishStatusAsync("IN_SERVICE");
                Log("Caisse vidée → EN_SERVICE", ConsoleColor.Green);
            }
            else if (CanBeInService())
            {
                Log("Caisse vidée — déjà EN_SERVICE", ConsoleColor.Green);
            }
            else
            {
                Log("Caisse vidée — autres conditions bloquantes",
                    ConsoleColor.Yellow);
            }
        }

        public async Task RefillPaperAsync()
        {
            _state.PaperLevel = 100;
            _state.PaperAlertSent = false;
            _state.PaperEmptyAlertSent = false;
            SaveState(); 

            await PublishEventAsync(MachineEventType.PAPER_REFILLED);

            // machine evaluates and reports its own status
            if (CanBeInService() && _state.Status != "IN_SERVICE")
            {
                _state.Status = "IN_SERVICE";
                await PublishStatusAsync("IN_SERVICE");
                Log("Papier rechargé → EN_SERVICE",
                    ConsoleColor.Green);
            }
            else if (CanBeInService())
            {
                Log("Papier rechargé — déjà EN_SERVICE", ConsoleColor.Green);
            }
            else
            {
                Log("Papier rechargé — autres conditions bloquantes",
                    ConsoleColor.Yellow);
            }
        }

        public async Task CloseDoorAsync()
        {
            _state.DoorOpen = false;
            SaveState();


            await PublishEventAsync(MachineEventType.DOOR_CLOSED);

            // machine evaluates and reports its own status
            if (CanBeInService() && _state.Status != "IN_SERVICE")
            {
                _state.Status = "IN_SERVICE";
                await PublishStatusAsync("IN_SERVICE");
                Log("Porte fermée → EN_SERVICE",
                    ConsoleColor.Green);
            }
            else if (CanBeInService())
            {
                Log("Porte fermée — déjà EN_SERVICE", ConsoleColor.Green);
            }
            else
            {
                Log("Porte fermée — autres conditions bloquantes",
                    ConsoleColor.Yellow);
            }
        }

        public async Task FixHardwareAsync()
        {
            _state.HardwareFailure = false;
            SaveState(); 


            await PublishEventAsync(MachineEventType.HARDWARE_FIXED);

            Log("Hardware failure cleared", ConsoleColor.Green);

            if (CanBeInService() && _state.Status != "IN_SERVICE")
            {
                _state.Status = "IN_SERVICE";
                await PublishStatusAsync("IN_SERVICE");
                Log("Hardware fixed → EN_SERVICE", ConsoleColor.Green);
            }
            else if (CanBeInService())
            {
                Log("Hardware fixed — déjà EN_SERVICE", ConsoleColor.Green);
            }
            else
            {
                Log("Hardware fixed — autres conditions bloquantes", ConsoleColor.Yellow);
            }
        }

        public static void RestoreStateFromFile(ParkMeterState state)
        {
            try
            {
                var statePath = $"state_{state.StateFilePrefix}_{state.SerialNumber}.json";
                if (!File.Exists(statePath)) return;

                var json = File.ReadAllText(statePath);
                var saved = JsonSerializer.Deserialize<JsonElement>(json);

                state.CashAmount = saved.GetProperty("CashAmount").GetDecimal();
                state.CashBoxAlertSent = saved.GetProperty("CashBoxAlertSent").GetBoolean();
                state.CashBoxWarnSent = saved.GetProperty("CashBoxWarnSent").GetBoolean();
                state.PaperLevel = saved.GetProperty("PaperLevel").GetInt32();
                state.PaperEmptyAlertSent = saved.GetProperty("PaperEmptyAlertSent").GetBoolean();
                state.PaperAlertSent = saved.GetProperty("PaperAlertSent").GetBoolean();
                state.HardwareFailure = saved.GetProperty("HardwareFailure").GetBoolean();
                state.DoorOpen = saved.GetProperty("DoorOpen").GetBoolean();
            }
            catch { }
        }
        private bool CanBeInService()
        {
            var result = _state.HasConfig
                && _state.CashAmount < _state.CashFullThreshold
                && !_state.CashBoxAlertSent
                && _state.PaperLevel > 0
                && !_state.PaperEmptyAlertSent
                && !_state.HardwareFailure
                && !_state.DoorOpen
                && _state.Status != "OUT_OF_SERVICE";

            Log($"CanBeInService: {result} | " +
                $"Cash:{_state.CashAmount:F1} | " +
                $"CashAlert:{_state.CashBoxAlertSent} | " +
                $"Paper:{_state.PaperLevel} | " +
                $"PaperAlert:{_state.PaperEmptyAlertSent} | " +
                $"HW:{_state.HardwareFailure} | " +
                $"Door:{_state.DoorOpen} | " +
                $"Status:{_state.Status}",
                ConsoleColor.DarkGray);

            return result;
        }

        private bool CanBeInServiceFromCommand()
        {
            return _state.HasConfig
                && _state.CashAmount < _state.CashFullThreshold
                && _state.PaperLevel > 0
                && !_state.CashBoxAlertSent
                && !_state.PaperEmptyAlertSent
                && !_state.HardwareFailure
                && !_state.DoorOpen;
        }

        //  Connection 

        private async Task<bool> ConnectWithRetryAsync(
            CancellationToken ct)
        {
            var options = new MqttClientOptionsBuilder()
              //------------- SWITCH FOR DEMO  ---------------------

              //.WithTcpServer("localhost", 1883) //local
                .WithTcpServer("134.112.18.30", 1883) //cloud
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
                catch (OperationCanceledException)
                {
                    return false;
                }
                catch
                {
                    Log($"Connection attempt {attempt}/5...",
                        ConsoleColor.Yellow);
                    await Task.Delay(2000, ct);
                }
            }

            Log("Could not connect after 5 attempts.",
                ConsoleColor.Red);
            return false;
        }

        private void SaveState()
        {
            try
            {
                var statePath = $"state_{_state.StateFilePrefix}_{_state.SerialNumber}.json";
                var stateToSave = new
                {
                    CashAmount = _state.CashAmount,
                    CashBoxAlertSent = _state.CashBoxAlertSent,
                    CashBoxWarnSent = _state.CashBoxWarnSent,
                    PaperLevel = _state.PaperLevel,
                    PaperEmptyAlertSent = _state.PaperEmptyAlertSent,
                    PaperAlertSent = _state.PaperAlertSent,
                    HardwareFailure = _state.HardwareFailure,
                    DoorOpen = _state.DoorOpen
                };
                File.WriteAllText(statePath,
                    JsonSerializer.Serialize(stateToSave));
            }
            catch { }
        }
        private void RegisterMessageListener(CancellationToken ct)
        {
            _client!.DisconnectedAsync += async e =>
            {
                if (ct.IsCancellationRequested) return;
                if (_state.IsIntentionalDisconnect) return; 
                if (_state.IsOffline && !_state.IsConnectionLost)
                    return;

                Log("Unexpected disconnection — will reconnect...",
                    ConsoleColor.Yellow);

                _state.IsConnectionLost = true;
                _state.IsOffline = true;
                _state.WasConnectionLost = true;
            };

            _client!.ApplicationMessageReceivedAsync += e =>
            {
                var topic = e.ApplicationMessage.Topic;
                var payload = e.ApplicationMessage
                    .ConvertPayloadToString();

                try
                {
                    if (topic.EndsWith("/config"))
                        ApplyConfig(payload);
                    else if (topic.EndsWith("/command"))
                        ApplyCommand(payload);
                }
                catch (Exception ex)
                {
                    Log($"Message error: {ex.Message}",
                        ConsoleColor.Red);
                }

                return Task.CompletedTask;
            };
        }

        // config reception 

        private void ApplyConfig(string payload)
        {
            Console.WriteLine($"[CONFIG] {_state.SerialNumber} received config");

            var raw = JsonSerializer.Deserialize<JsonElement>(
                payload,
                new JsonSerializerOptions
                { PropertyNameCaseInsensitive = true });

            _state.MaxDuration = raw
                .GetProperty("maxDuration").GetInt32();

            var pricing = raw.GetProperty("pricing");
            _state.StrategyType = pricing
                .GetProperty("strategy").GetString()!;

            var parameters = pricing.GetProperty("parameters");

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

            if (_state.Status == "PENDING_ACTIVATION")
            {
                Console.WriteLine($"[CONFIG] HasConfig={_state.HasConfig}, CanBeInService={CanBeInService()}");

                if (CanBeInService())
                {
                    _state.Status = "IN_SERVICE";
                    Console.WriteLine($"[CONFIG] → Publishing IN_SERVICE");

                    _ = PublishStatusAsync("IN_SERVICE");
                    Log("Config applied → EN_SERVICE", ConsoleColor.Green);
                }
            }
        }


        // command reception (management decisions)

        private void ApplyCommand(string payload)
        {
            var raw = JsonSerializer.Deserialize<JsonElement>(
                payload,
                new JsonSerializerOptions
                { PropertyNameCaseInsensitive = true });

            var command = raw.GetProperty("command").GetString()!;

            Log($"Command received: {command}", ConsoleColor.Magenta);

            switch (command)
            {
                case "SET_OUT_OF_SERVICE":
                    if (_state.Status == "OUT_OF_SERVICE")
                    {
                        Log("Already OUT_OF_SERVICE — command ignored",
                            ConsoleColor.Yellow);
                        _ = PublishStatusAsync(_state.Status); 
                        break;
                    }
                    _state.Status = "OUT_OF_SERVICE";
                     _ = PublishStatusAsync("OUT_OF_SERVICE");
                    Log("Command applied → HORS_SERVICE", ConsoleColor.Red);
                    break;

                case "SET_IN_SERVICE":
                    if (CanBeInServiceFromCommand()) 
                    {
                        _state.Status = "IN_SERVICE";
                        _ = PublishStatusAsync("IN_SERVICE");
                        Log("Command applied → EN_SERVICE", ConsoleColor.Green);
                    }
                    else
                    {
                        _ = PublishStatusAsync(_state.Status);
                        Log("Command SET_IN_SERVICE rejected — " +
                            "physical conditions not met",
                            ConsoleColor.Yellow);
                    }
                    break;

                default:
                    Log($"Unknown command: {command}",
                        ConsoleColor.Yellow);
                    break;
            }
        }

        // simulation loop 

        private async Task RunSimulationLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    if (_state.IsIntentionalDisconnect)
                        return;

                    if (_state.IsOffline)
                    {
                        Log("Disconnecting...",
                            ConsoleColor.Red);
                        await StopAsync();
                        return;
                    }

                    if (!_state.HasConfig)
                        Log("No configuration — waiting for config push...",
                            ConsoleColor.Yellow);
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
                default:
                    Log($"Status: {_state.Status} — " +
                        $"waiting for activation...",
                        ConsoleColor.Yellow);
                    break;
            }
        }

        private async Task HandleInServiceAsync()
        {

            if (_state.IsIntentionalDisconnect || _state.IsOffline)
                return;

            if (_state.CashAmount >= _state.CashFullThreshold)
            {
                Log($"Cash box full ({_state.CashAmount:F3} DT) — " +
                    "waiting for emptying...",
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

            var (amount, duration, status) =
                await SendTransactionAsync();
            if (_state.Status == "IN_SERVICE")
            { 
                UpdateState(status);
            LogTransaction(amount, duration, status);
            await PublishTelemetryAsync();
            }
        }

        // transactions 

        private async Task<(decimal amount, int duration,
            TransactionStatus status)> SendTransactionAsync()
        {
            if (_state.Status != "IN_SERVICE")
            {
                Log("Status changed — skipping transaction",
                    ConsoleColor.Yellow);
                return (0, 0, TransactionStatus.CANCELLED);
            }

            var duration = _random.Next(15, _state.MaxDuration);
            var now = DateTime.UtcNow;
            var status = GetTransactionStatus();
            var amount = status == TransactionStatus.COMPLETED
                ? CalculateAmount(duration, now)
                : 0;

            if (status == TransactionStatus.COMPLETED)
                _state.CashAmount += amount;

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
                _ => TransactionStatus.CANCELLED
            };
        }

        private void LogTransaction(
            decimal amount, int duration, TransactionStatus status)
        {
            var color = status == TransactionStatus.COMPLETED
                ? ConsoleColor.Green
                : ConsoleColor.Yellow;

            Log($"Transaction: {amount:F3} DT / {duration} min | " +
                $"Status: {status} | " +
                $"Cash: {_state.CashAmount:F3} DT | " +
                $"Paper: {_state.PaperLevel}%",
                color);
        }

        private void UpdateState(TransactionStatus status)
        {
            _state.TransactionCount++;

            if (status == TransactionStatus.COMPLETED)
            {
                _state.PaperLevel = Math.Max(0,
                    _state.PaperLevel - _random.Next(
                        _state.PaperUsePerTick - 1,
                        _state.PaperUsePerTick + 2));
            }
            SaveState();
        }

        private decimal CalculateAmount(
            int durationMinutes, DateTime time)
        {
            return _state.StrategyType switch
            {
                "TIME_OF_DAY" =>
                    time.Hour >= 8 && time.Hour < 18
                        ? Math.Round(
                            _state.PeakRate * durationMinutes / 60, 3)
                        : Math.Round(
                            _state.OffPeakRate * durationMinutes / 60, 3),

                "WEEKDAY_WEEKEND" =>
                    time.DayOfWeek == DayOfWeek.Saturday ||
                    time.DayOfWeek == DayOfWeek.Sunday
                        ? Math.Round(
                            _state.WeekendRate * durationMinutes / 60, 3)
                        : Math.Round(
                            _state.WeekdayRate * durationMinutes / 60, 3),

                _ => Math.Round(
                    _state.PricePerHour * durationMinutes / 60, 3)
            };
        }

        // alert checking 

        private async Task CheckAndSendAlertsAsync()
        {
            await CheckCashBoxAsync();
            await CheckPaperAsync();
            await CheckRandomHardwareAsync();
        }

        private async Task CheckCashBoxAsync()
        {
            if (_state.CashAmount >= _state.CashWarnThreshold
                && !_state.CashBoxWarnSent)
                await SendAlertAndUpdateStateAsync(AlertType.CASH_BOX_WARN);

            if (_state.CashAmount >= _state.CashFullThreshold
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
            if (!_state.HardwareFailure &&
                _random.Next(HARDWARE_FAILURE_CHANCE) == 0)
                await SendAlertAndUpdateStateAsync(
                    AlertType.HARDWARE_FAILURE);
        }

        private async Task SendAlertAndUpdateStateAsync(
            AlertType alertType)
        {
            await SendAlertAsync(alertType);
            UpdateLocalState(alertType);
        }

        private void UpdateLocalState(AlertType alertType)
        {
            if (alertType.HasFlag(AlertType.CASH_BOX_WARN))
            {
                if (_state.CashAmount < _state.CashWarnThreshold)
                    _state.CashAmount = _state.CashWarnThreshold;
                _state.CashBoxWarnSent = true;
                Log($"CASH_BOX_WARN ({_state.CashAmount:F3} DT)",
                    ConsoleColor.Yellow);
            }

            if (alertType.HasFlag(AlertType.CASH_BOX_FULL))
            {
                _state.CashAmount = _state.CashFullThreshold;
                _state.CashBoxWarnSent = true;
                _state.CashBoxAlertSent = true;
                if (_state.Status != "TEMPORARILY_UNAVAILABLE" && _state.Status != "OUT_OF_SERVICE") 
                {
                    _state.Status = "TEMPORARILY_UNAVAILABLE";
                    _ = PublishStatusAsync("TEMPORARILY_UNAVAILABLE");
                }
                Log($"CASH_BOX_FULL", ConsoleColor.Red);
            }

            if (alertType.HasFlag(AlertType.PAPER_LOW))
            {
                _state.PaperLevel = Math.Min(_state.PaperLevel, 15);
                _state.PaperAlertSent = true;
                Log($"PAPER_LOW ({_state.PaperLevel}%)",
                    ConsoleColor.Yellow);
            }

            if (alertType.HasFlag(AlertType.PAPER_EMPTY))
            {
                _state.PaperLevel = 0;
                _state.PaperAlertSent = true;
                _state.PaperEmptyAlertSent = true;

                if (_state.Status != "TEMPORARILY_UNAVAILABLE" && _state.Status != "OUT_OF_SERVICE")
                {
                    _state.Status = "TEMPORARILY_UNAVAILABLE";
                    _ = PublishStatusAsync("TEMPORARILY_UNAVAILABLE");
                }

                Log("PAPER_EMPTY",
                    ConsoleColor.Red);
            }

            if (alertType.HasFlag(AlertType.HARDWARE_FAILURE))
            {
                _state.HardwareFailure = true;

                if (_state.Status != "TEMPORARILY_UNAVAILABLE" && _state.Status != "OUT_OF_SERVICE")
                {
                    _state.Status = "TEMPORARILY_UNAVAILABLE";
                    _ = PublishStatusAsync("TEMPORARILY_UNAVAILABLE");
                }

                Log("HARDWARE_FAILURE",
                    ConsoleColor.Red);
            }

            if (alertType.HasFlag(AlertType.DOOR_OPEN))
            {
                _state.DoorOpen = true;

                if (_state.Status != "TEMPORARILY_UNAVAILABLE" && _state.Status != "OUT_OF_SERVICE")
                {
                    _state.Status = "TEMPORARILY_UNAVAILABLE";
                    _ = PublishStatusAsync("TEMPORARILY_UNAVAILABLE");
                }

                Log("DOOR_OPEN",
                    ConsoleColor.Yellow);
            }

            if (alertType.HasFlag(AlertType.CONNECTION_LOST))
            {
                _state.IsOffline = true;
                _state.IsConnectionLost = true;
                _state.WasConnectionLost = true;
                Log("CONNECTION_LOST — disconnecting, will reconnect...",
                    ConsoleColor.Yellow);
            }
            SaveState();

        }

        //  MQTT helpers 

        private async Task PublishEventAsync(
            MachineEventType eventType)
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

        private async Task PublishStatusAsync(string status)
        {
            // machine reports its own status
            await PublishAsync(
                $"parkmeters/{_state.Id}/status",
                new
                {
                    ParkmeterId = _state.Id.ToString(),
                    Status = status,
                    CreatedAt = DateTime.UtcNow.ToString("O")
                });

            Log($"Status published → {status}",
                ConsoleColor.DarkCyan);
        }

        private async Task PublishTelemetryAsync()
        {
            await PublishAsync(
                $"parkmeters/{_state.Id}/telemetry",
                new
                {
                    ParkmeterId = _state.Id.ToString(),
                    CashAmount = _state.CashAmount,      
                    TransactionCount = _state.TransactionCount,
                    CreatedAt = DateTime.UtcNow.ToString("O")
                });
        }

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
                Log($"[OFFLINE] Cannot publish to {topic}",
                    ConsoleColor.DarkYellow);
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
                _state.IsConnectionLost = true;
                _state.WasConnectionLost = true;
            }
        }

        // logs

        private void Log(string message, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine($"[{_state.SerialNumber}] {message}");
            Console.ResetColor();
        }
    }
}
