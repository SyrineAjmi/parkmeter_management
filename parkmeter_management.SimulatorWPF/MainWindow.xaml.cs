using parkmeter_management.Domain.Enums;
using parkmeter_management.SimulatorCore;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using static System.Net.WebRequestMethods;

namespace SimulatorWPF
{
    public partial class MainWindow : Window
    {
        // ── Constants ─────────────────────────────────────────
        private const string API_URL = "https://localhost:7016";

        // ── State ─────────────────────────────────────────────
        private readonly HttpClient _http;
        private readonly JsonSerializerOptions _jsonOpts;
        private List<ParkMeterInfo> _parkmeters = new();
        private readonly Dictionary<Guid, ParkMeterSimulator> _simulators = new();
        private readonly Dictionary<Guid, Task> _simTasks = new();
        private readonly Dictionary<Guid, ParkmeterCard> _cards = new();
        private CancellationTokenSource _cts = new();
        private readonly DispatcherTimer _uiTimer;

        public MainWindow()
        {
            InitializeComponent();

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (m, c, ch, e) => true
            };
            _http = new HttpClient(handler) { BaseAddress = new Uri(API_URL + "/") };
            _jsonOpts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // UI refresh timer
            _uiTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _uiTimer.Tick += OnUiTick;
            _uiTimer.Start();

            Loaded += OnLoaded;
            Closing += OnClosing;
        }

        // ── Startup ───────────────────────────────────────────

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            StatusText.Text = "Connexion à l'API...";
            await WaitForApiAsync();
            await LoadParkmetersAsync();
        }

        private async Task WaitForApiAsync()
        {
            while (true)
            {
                try
                {
                    var response = await _http.GetAsync("health");
                    if (response.IsSuccessStatusCode)
                    {
                        AddLog("SYSTEM", "API prête ", LogType.Success);
                        return;
                    }
                }
                catch { }

                Dispatcher.Invoke(() =>
                    StatusText.Text = "En attente de l'API...");
                await Task.Delay(1000);
            }
        }

        private async Task LoadParkmetersAsync()
        {
            try
            {
                var response = await _http.GetAsync("api/Parkmeters");
                var json = await response.Content.ReadAsStringAsync();
                _parkmeters = JsonSerializer.Deserialize<List<ParkMeterInfo>>(
                    json, _jsonOpts) ?? new();

                // build cards on UI thread
                Dispatcher.Invoke(() =>
                {
                    CardsPanel.Children.Clear();
                    _cards.Clear();

                    foreach (var pm in _parkmeters)
                    {
                        var card = new ParkmeterCard(
                            pm,
                            async (p) => await PowerOnAsync(p), 
                            TriggerAlert,
                            OperatorAction); _cards[pm.Id] = card;
                        CardsPanel.Children.Add(card);
                    }

                    StatusText.Text = $"MQTT localhost:1883  |  {_parkmeters.Count} parcmètres";
                    AddLog("SYSTEM", $"{_parkmeters.Count} parcmètres chargés", LogType.Success);
                });



                // auto start all parkmeters OUTSIDE Dispatcher.Invoke

                var index = 0;
                foreach (var pm in _parkmeters)
                {
                    await PowerOnAsync(pm, index);
                    index++;
                    await Task.Delay(200);
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    StatusText.Text = "Erreur de connexion API";
                    AddLog("SYSTEM", $"Erreur: {ex.Message}", LogType.Error);
                });
            }
        }

        // ── UI Timer ──────────────────────────────────────────

        private void OnUiTick(object? sender, EventArgs e)
        {
            foreach (var (id, sim) in _simulators)
            {
                if (_cards.TryGetValue(id, out var card))
                    card.UpdateState(sim);
            }
        }

        // ── Power ON ──────────────────────────────────────────

        private async Task PowerOnAsync(ParkMeterInfo pm, int index = 0)
        {
            if (_simulators.ContainsKey(pm.Id)) return;

            var response = await _http.GetAsync($"api/Parkmeters/{pm.Id}");
            ParkMeterInfo? fresh = null;
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                fresh = JsonSerializer.Deserialize<ParkMeterInfo>(json, _jsonOpts);
            }

            var currentCash = fresh?.CashAmount ?? 0;
            var currentStatus = fresh?.Status ?? "PENDING_ACTIVATION";
            var tickVariation = index * 3000; 

            var state = new ParkMeterState
            {
                Id = pm.Id,
                SerialNumber = pm.SerialNumber,
                Status = currentStatus,
                HasConfig = false,
                //CashAmount = currentCash,
                //CashBoxAlertSent = currentCash >= 40.000m,
                //CashBoxWarnSent = currentCash >= 20.000m,
                MinTickDelayMs = 10000 + tickVariation,
                MaxTickDelayMs = 20000 + tickVariation,
                PaperUsePerTick = 3
            };

            ParkMeterSimulator.RestoreStateFromFile(state); 

            var simulator = new ParkMeterSimulator(state);
            _simulators[pm.Id] = simulator;
            _simTasks[pm.Id] = simulator.RunAsync(_cts.Token);

            AddLog(pm.SerialNumber,
                $"Démarrage | Status: {currentStatus} | Cash: {currentCash:F3} DT",
                LogType.Info);

            ParkMeterSimulator.RestoreStateFromFile(state);
        }


        // ── Trigger Alert ─────────────────────────────────────

        private async void TriggerAlert(ParkMeterInfo pm, AlertType alertType)
        {
            if (!_simulators.TryGetValue(pm.Id, out var sim)) return;
            await sim.TriggerAlertAsync(alertType);
            AddLog(pm.SerialNumber, $"Alerte déclenchée: {alertType}", LogType.Error);
        }

        // ── Operator Action ───────────────────────────────────

        private async void OperatorAction(ParkMeterInfo pm, string action)
        {
            if (!_simulators.TryGetValue(pm.Id, out var sim)) return;

            switch (action)
            {
                case "empty":
                    await sim.EmptyCashBoxAsync();
                    AddLog(pm.SerialNumber, "Caisse vidée", LogType.Success);
                    break;
                case "paper":
                    await sim.RefillPaperAsync();
                    AddLog(pm.SerialNumber, "Papier rechargé", LogType.Success);
                    break;
                case "door":
                    await sim.CloseDoorAsync();
                    AddLog(pm.SerialNumber, "Porte fermée", LogType.Success);
                    break;
                case "hardware":
                    await sim.FixHardwareAsync();
                    AddLog(pm.SerialNumber, "Hardware réparé", LogType.Success);
                    break;
            }
        }

        // ── Log ───────────────────────────────────────────────

        public enum LogType { Info, Success, Warning, Error }

        private void AddLog(string serial, string message, LogType type)
        {
            Dispatcher.Invoke(() =>
            {
                var color = type switch
                {
                    LogType.Success => "#10B981",
                    LogType.Warning => "#F59E0B",
                    LogType.Error => "#EF4444",
                    _ => "#6366F1"
                };

                var entry = new Grid { Margin = new Thickness(0, 0, 0, 3) };
                entry.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
                entry.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(48) });
                entry.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var time = new TextBlock
                {
                    Text = DateTime.Now.ToString("HH:mm:ss"),
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 9,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x1E, 0x29, 0x3B)),
                    VerticalAlignment = VerticalAlignment.Top
                };

                var serialBlock = new TextBlock
                {
                    Text = serial,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 9,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)),
                    VerticalAlignment = VerticalAlignment.Top
                };

                var msg = new TextBlock
                {
                    Text = message,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 9,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x47, 0x55, 0x69)),
                    TextWrapping = TextWrapping.Wrap,
                    VerticalAlignment = VerticalAlignment.Top
                };

                Grid.SetColumn(time, 0);
                Grid.SetColumn(serialBlock, 1);
                Grid.SetColumn(msg, 2);

                entry.Children.Add(time);
                entry.Children.Add(serialBlock);
                entry.Children.Add(msg);

                LogPanel.Children.Insert(0, entry);

                // keep max 150 entries
                while (LogPanel.Children.Count > 150)
                    LogPanel.Children.RemoveAt(LogPanel.Children.Count - 1);
            });
        }

        // ── Shutdown ──────────────────────────────────────────

        private async void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _uiTimer.Stop();
            _cts.Cancel();
            if (_simTasks.Count > 0)
            {
                try { await Task.WhenAll(_simTasks.Values); }
                catch { }
            }
        }
    }

    // ── ParkMeter Info Model ──────────────────────────────────

    public class ParkMeterInfo
    {
        public Guid Id { get; set; }
        public string SerialNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal CashAmount { get; set; } = 0;
        public string? SiteName { get; set; }
    }

}
