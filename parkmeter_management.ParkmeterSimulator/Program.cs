using MQTTnet;
using MQTTnet.Client;
using parkmeter_management.Domain.Enums;
using parkmeter_management.SimulatorCore;
using System.Text.Json;



Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("ParkMeter Simulator Starting...");
Console.WriteLine("Waiting for API + broker...");
await Task.Delay(5000);

// ── http client ───────────────────────────────────────
var handler = new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = (m, c, ch, e) => true
};

var http = new HttpClient(handler)
{
    BaseAddress = new Uri("https://localhost:7016/") 
};


// ── json options ──────────────────────────────────────
var jsonOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true
};


// ── startup ───────────────────────────────────────────
var parkmeters = await FetchParkmetersAsync(http);
if (parkmeters.Count == 0) return;

var runningCount = 0;
var simulators = new Dictionary<Guid, ParkMeterSimulator>();
var simTasks = new Dictionary<Guid, Task>();
var cts = new CancellationTokenSource();

_ = StartNewParkmeterListenerAsync(http, parkmeters, cts.Token);
PrintDemoMenu(parkmeters, runningCount, simulators);


// ── main loop ─────────────────────────────────────────
while (true)
{
    // Auto-detect offline parkmeters
    foreach (var pm in parkmeters.ToList())
    {
        if (!simulators.ContainsKey(pm.Id)) continue;

        var sim = simulators[pm.Id];
        if (sim.IsOffline && !sim.IsConnectionLost)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[POWER] {pm.SerialNumber} " +
                $"permanently offline — press [+] to restart");
            Console.ResetColor();

            simulators.Remove(pm.Id);
            simTasks.Remove(pm.Id);
            runningCount = simulators.Count;
            PrintDemoMenu(parkmeters, runningCount, simulators);
        }
    }

    if (!Console.KeyAvailable)
        {
            await Task.Delay(100);
            continue;
        }

    var key = Console.ReadKey(intercept: true).Key;

    if (key == ConsoleKey.Q) { await ShutdownAsync(cts, simTasks); break; }
    if (key == ConsoleKey.M) { PrintDemoMenu(parkmeters, runningCount, simulators); continue; }
    if (key == ConsoleKey.S)
    {
        await PrintSimulatorStatus(parkmeters, simulators, runningCount,http);
        continue;
    }
    if (key == ConsoleKey.G)
    {
        if (simulators.Count == 0)
        { LogWarning("No parkmeters running!"); continue; }
        await RunAgentModeAsync(parkmeters, simulators, runningCount);
        //PrintDemoMenu(parkmeters, runningCount, simulators);
        continue;
    }
    if (key == ConsoleKey.T)
    {
        if (simulators.Count == 0)
        { LogWarning("No parkmeters running!"); continue; }
        await RunTestModeAsync(parkmeters, simulators, runningCount);
        //PrintDemoMenu(parkmeters, runningCount, simulators);
        continue;
    }
    if (key == ConsoleKey.OemPlus
     || key == ConsoleKey.Add) { await PowerOnAsync(http, parkmeters, simulators, simTasks, cts); runningCount = simulators.Count; 
        //PrintDemoMenu(parkmeters, runningCount, simulators); 
        continue; }
    if (key == ConsoleKey.OemMinus
     || key == ConsoleKey.Subtract) { await PowerOffAsync(parkmeters, simulators, simTasks); runningCount = simulators.Count; 
        //PrintDemoMenu(parkmeters, runningCount,simulators);
        continue; } 
}


// ── shutting down ──────────────────────────────────────────
async Task ShutdownAsync(
    CancellationTokenSource cts,
    Dictionary<Guid, Task> tasks) 
{
    Console.WriteLine("\nShutting down all parkmeters...");
    cts.Cancel();
    if (tasks.Count == 0) return;
    try { await Task.WhenAll(tasks.Values); } 
    catch (OperationCanceledException)
    { Console.WriteLine("All parkmeters stopped."); }
}

// ── powering on the parkmeters ──────────────────────────────────────
async Task PowerOnAsync(
    HttpClient http,
    List<ParkMeterInfo> pms,
    Dictionary<Guid, ParkMeterSimulator> sims,
    Dictionary<Guid, Task> tasks,
    CancellationTokenSource cts)
{
    // find next parkmeter NOT already running
    var pm = pms.FirstOrDefault(p => !sims.ContainsKey(p.Id));
    if (pm == null)
    { LogWarning($"All {pms.Count} parkmeters already running!"); return; }

    var config = await FetchConfigAsync(http, pm.Id);
    var activeAlerts = await FetchActiveAlertsAsync(http, pm.Id);
    var freshPm = await FetchParkmeterByIdAsync(http, pm.Id);
    var currentStatus = freshPm?.Status ?? pm.Status;

    if (currentStatus == "OUT_OF_SERVICE")
    {
        LogWarning($"{pm.SerialNumber} is OUT_OF_SERVICE — " +
            $"activate it first!");
        return;
    }

    if (freshPm != null)
    {
        var idx = pms.FindIndex(p => p.Id == pm.Id);
        if (idx >= 0) pms[idx] = freshPm;
    }

    var isBlocked = freshPm?.Status == "TEMPORARILY_UNAVAILABLE"
             || freshPm?.Status == "OUT_OF_SERVICE";

    var state = new ParkMeterState
    {
        Id = pm.Id,
        SerialNumber = pm.SerialNumber,
        Status = freshPm?.Status ?? pm.Status,
        PricePerHour = config?.PricePerHour ?? 0,
        MaxDuration = config?.MaxDuration ?? 0,
        HasConfig = config != null,
        MinTickDelayMs = 15000,
        MaxTickDelayMs = 30000,
        CashFillPerTick = 8,
        PaperUsePerTick = 3,

        HardwareFailure = activeAlerts.Contains("HARDWARE_FAILURE"),
        DoorOpen = activeAlerts.Contains("DOOR_OPEN"),
        CashBoxWarnSent = activeAlerts.Contains("CASH_BOX_WARN")
                           || activeAlerts.Contains("CASH_BOX_FULL"),
        CashBoxAlertSent = activeAlerts.Contains("CASH_BOX_FULL"),
        PaperAlertSent = activeAlerts.Contains("PAPER_LOW")
                           || activeAlerts.Contains("PAPER_EMPTY"),
        PaperEmptyAlertSent = activeAlerts.Contains("PAPER_EMPTY"),

        CashLevel = activeAlerts.Contains("CASH_BOX_FULL") ? 100 :
                     activeAlerts.Contains("CASH_BOX_WARN") ? 85 : 0,
        PaperLevel = activeAlerts.Contains("PAPER_EMPTY") ? 0 :
                     activeAlerts.Contains("PAPER_LOW") ? 15 : 100
    };

    var simulator = new ParkMeterSimulator(state);
    sims[pm.Id] = simulator;
    tasks[pm.Id] = simulator.RunAsync(cts.Token);

    LogDemo($"[+] {pm.SerialNumber} powered ON | " +
        $"Status: {state.Status} | " +
        $"Config: {(config != null ? $"{state.PricePerHour} DT/h | Max: {state.MaxDuration} min" : "none")}");
}


// ── powering off the parkmeters ──────────────────────────────────
async Task PowerOffAsync(
    List<ParkMeterInfo> pms,
    Dictionary<Guid, ParkMeterSimulator> sims,
    Dictionary<Guid, Task> tasks)
{
    if (sims.Count == 0)
    { LogWarning("No parkmeters running!"); return; }

    // power off last added simulator
    var lastPm = pms.LastOrDefault(p => sims.ContainsKey(p.Id));
    if (lastPm == null) return;

    await sims[lastPm.Id].StopAsync();
    LogDemo($"[-] {lastPm.SerialNumber} powered OFF");

    sims.Remove(lastPm.Id);
    tasks.Remove(lastPm.Id);
}

// ── listen to new added parkmeters ────────────────────────────
async Task StartNewParkmeterListenerAsync(
    HttpClient http,
    List<ParkMeterInfo> parkmeters,
    CancellationToken ct)
{
    var client = new MqttFactory().CreateMqttClient();
    var options = new MqttClientOptionsBuilder()
        .WithTcpServer("localhost", 1883)
        .WithClientId("simulator-system-listener")
        .Build();

    try { await client.ConnectAsync(options, ct); }
    catch
    {
        LogWarning("Could not connect system listener");
        return;
    }

    client.ApplicationMessageReceivedAsync += async e =>
    {
        if (e.ApplicationMessage.Topic != "system/parkmeter/new")
            return;

        await HandleNewParkmeterNotificationAsync(
            http, parkmeters,
            e.ApplicationMessage.ConvertPayloadToString());
    };

    await client.SubscribeAsync("system/parkmeter/new",
        cancellationToken: ct);
}

// new added parkmeter
async Task HandleNewParkmeterNotificationAsync(
    HttpClient http,
    List<ParkMeterInfo> parkmeters,
    string payload)
{
    try
    {
        var raw = JsonSerializer.Deserialize<JsonElement>(payload);
        var id = Guid.Parse(raw.GetProperty("Id").GetString()!);
        var serialNumber = raw.GetProperty("SerialNumber").GetString()!;

        if (parkmeters.Any(p => p.Id == id)) return;

        var pmInfo = await FetchParkmeterByIdAsync(http, id);
        if (pmInfo == null) return;

        parkmeters.Add(pmInfo);

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n[SYNC] New parkmeter detected: {serialNumber}");
        Console.WriteLine($"   - Press [+] to power it on");
        Console.ResetColor();
    }
    catch (Exception ex)
    {
        LogWarning($"Sync error: {ex.Message}");
    }
}


// ── helpers for fetching ─────────────────────────────────────
async Task<List<ParkMeterInfo>> FetchParkmetersAsync(HttpClient http)
{
    var response = await http.GetAsync("api/Parkmeters");
    var json = await response.Content.ReadAsStringAsync();
    var result = JsonSerializer.Deserialize<List<ParkMeterInfo>>(
        json, jsonOptions) ?? new List<ParkMeterInfo>();

    Console.WriteLine($" {result.Count} parkmeters loaded from API");
    return result;
}

async Task<ParkMeterInfo?> FetchParkmeterByIdAsync(
    HttpClient http, Guid id)
{
    var response = await http.GetAsync($"api/Parkmeters/{id}");
    if (!response.IsSuccessStatusCode) return null;

    var json = await response.Content.ReadAsStringAsync();
    return JsonSerializer.Deserialize<ParkMeterInfo>(json, jsonOptions);
}

async Task<ParkMeterConfigInfo?> FetchConfigAsync(
    HttpClient http, Guid parkmeterID)
{
    var response = await http.GetAsync(
        $"api/ParkmeterConfiguration/{parkmeterID}");
    if (!response.IsSuccessStatusCode) return null;

    var json = await response.Content.ReadAsStringAsync();
    return JsonSerializer.Deserialize<ParkMeterConfigInfo>(
        json, jsonOptions);
}

async Task RunTestModeAsync(
    List<ParkMeterInfo> pms,
    Dictionary<Guid, ParkMeterSimulator> sims,
    int running)
{
    
    // ── select alerts ─────────────────────────
    var selectedAlerts = await SelectAlertsAsync();
    if (selectedAlerts == AlertType.NONE)
    { LogWarning("No alerts selected."); return; }

    // hardware alerts allowed regardless of config/status
    var hardwareOnly =
        AlertType.DOOR_OPEN |
        AlertType.HARDWARE_FAILURE |
        AlertType.POWER_FAILURE |
        AlertType.CONNECTION_LOST |
        AlertType.CASH_BOX_REMOVED;
   

    // select source ─────────────────────────
    PrintTestSourceMenu(pms, sims);
    var sourceKey = Console.ReadKey(intercept: true).Key;
    Console.WriteLine();

    List<(ParkMeterSimulator sim, string serial)> targets = new();

    if (sourceKey == ConsoleKey.A)
    {
        targets = pms
            .Where(p => sims.ContainsKey(p.Id))
            .Select(p => (sims[p.Id], p.SerialNumber))
            .ToList(); Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("  → ALL parkmeters selected");
        Console.ResetColor();
    }
    else
    {
        var runningPms = pms.Where(p => sims.ContainsKey(p.Id)).ToList();
        var idx = GetParkmeterIndex(sourceKey);
        if (idx == null || idx >= runningPms.Count)
        { LogWarning("Invalid selection."); return; }
        var selectedPm = runningPms[idx.Value];
        targets.Add((sims[selectedPm.Id], selectedPm.SerialNumber));
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"  → {selectedPm.SerialNumber} selected");
        Console.ResetColor();
    }

    // ── send ──────────────────────────────────
    var anySent = false;

    await Task.WhenAll(targets.Select(async t =>
    {
        var allowedAlerts = selectedAlerts;

        if (!t.sim.HasConfig || t.sim.Status != "IN_SERVICE")
        {
            allowedAlerts = selectedAlerts & hardwareOnly;

            if (allowedAlerts == AlertType.NONE)
            {
                LogWarning(
                    $"— selected alerts require IN_SERVICE + config");
                return;
            }

            // what was stripped
            var stripped = selectedAlerts & ~hardwareOnly;
            LogWarning($"{t.serial} Sending alert(s)     : {allowedAlerts}");
        }

        await t.sim.TriggerAlertAsync(allowedAlerts);
        anySent = true; 

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"[TEST] {t.serial} - " +
            $"{allowedAlerts}");
        Console.ResetColor();
    }));

    // ── Result ────────────────────────────────────────
    if (anySent)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n[TEST] Done! Check API console for saved alerts"); 
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("\n[TEST] No alerts sent — " +
            "configure parkmeters and set them IN_SERVICE first");
    }
    Console.ResetColor();
}

    async Task<AlertType> SelectAlertsAsync()
{
    var available = new Dictionary<ConsoleKey,
        (AlertType type, string name)>
    {
        [ConsoleKey.D1] = (AlertType.CASH_BOX_WARN, "CASH_BOX_WARN"),
        [ConsoleKey.D2] = (AlertType.CASH_BOX_FULL, "CASH_BOX_FULL"),
        [ConsoleKey.D3] = (AlertType.PAPER_LOW, "PAPER_LOW"),
        [ConsoleKey.D4] = (AlertType.PAPER_EMPTY, "PAPER_EMPTY"),
        [ConsoleKey.D5] = (AlertType.HARDWARE_FAILURE, "HARDWARE_FAILURE"),
        [ConsoleKey.D6] = (AlertType.DOOR_OPEN, "DOOR_OPEN"),
        [ConsoleKey.D7] = (AlertType.POWER_FAILURE, "POWER_FAILURE"),
        [ConsoleKey.D8] = (AlertType.CONNECTION_LOST, "CONNECTION_LOST"),
    };


    var selected = new HashSet<AlertType>();

    while (true)
    {
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine("\n──────────────────────────────────────────────────────────────");
        Console.WriteLine("  TEST MODE — Select alert(s)");
        Console.WriteLine("  select with number keys, ENTER to confirm");
        Console.WriteLine("──────────────────────────────────────────────────────────────");

        Console.ForegroundColor = ConsoleColor.Magenta;
        foreach (var kvp in available)
        {
            var isSelected = selected.Contains(kvp.Value.type);
            Console.ForegroundColor = isSelected
                ? ConsoleColor.Green
                : ConsoleColor.Magenta;
            var indicator = isSelected ? "°" : "  ";
            Console.WriteLine($"  {indicator} [{kvp.Key.ToString().Replace("D", "")}] {kvp.Value.name}");
        }

        var combined = selected.Count > 0
            ? selected.Aggregate((a, b) => a | b)
            : AlertType.NONE;

        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine("────────────────────────────────────────");
        if (selected.Count > 0)
        {
            Console.WriteLine($"  Selected: {string.Join(" | ", selected)}");
        }
        else
            Console.WriteLine("No alerts selected yet");

        Console.WriteLine("────────────────────────────────────────");
        Console.WriteLine("  [ENTER] Confirm   [C] Clear all");
        Console.WriteLine("──────────────────────────────────────────────────────────────");

        Console.ResetColor();

        var key = Console.ReadKey(intercept: true).Key;
        Console.WriteLine();

        if (key == ConsoleKey.Enter)
        {
            if (selected.Contains(AlertType.POWER_FAILURE) &&
                selected.Contains(AlertType.CONNECTION_LOST))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(" POWER_FAILURE and CONNECTION_LOST cannot happen together!");
                Console.ResetColor();
                continue;
            }

            return combined;
        }

        if (key == ConsoleKey.C)
        { selected.Clear(); continue; }

        if (available.TryGetValue(key, out var alert))
        {
            if (selected.Contains(alert.type))
                selected.Remove(alert.type);
            else
                selected.Add(alert.type);
        }
    }
}

// physical actions (real)
async Task RunAgentModeAsync(
    List<ParkMeterInfo> pms,
    Dictionary<Guid, ParkMeterSimulator> sims,
    int running)
{
    Console.ForegroundColor = ConsoleColor.DarkCyan;
    Console.WriteLine("\n════════════════════════════════════════");
    Console.WriteLine("  AGENT MODE");
    Console.WriteLine("════════════════════════════════════════");

    // show only running parkmeters
    var runningPms = pms.Where(p => sims.ContainsKey(p.Id)).ToList();
    for (int i = 0; i < runningPms.Count; i++)
        Console.WriteLine($"    [{i + 1}] {runningPms[i].SerialNumber} " +
            $"({sims[runningPms[i].Id].Status})");
    Console.WriteLine("────────────────────────────────────────");
    Console.ResetColor();

    var pmKey = Console.ReadKey(intercept: true).Key;
    Console.WriteLine();

    var idx = GetParkmeterIndex(pmKey);
    if (idx == null || idx >= runningPms.Count)
    { LogWarning("Invalid selection."); return; }

    var selectedPm = runningPms[idx.Value];
    var selectedSim = sims[selectedPm.Id];

    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"  → {selectedPm.SerialNumber} selected");
    Console.ResetColor();

    Console.ForegroundColor = ConsoleColor.DarkCyan;
    Console.WriteLine("────────────────────────────────────────");
    Console.WriteLine("  Select action:");
    Console.WriteLine("    [E] Empty cash box");
    Console.WriteLine("    [F] Refill paper");
    Console.WriteLine("    [D] Close door");
    Console.WriteLine("────────────────────────────────────────");
    Console.ResetColor();

    var actionKey = Console.ReadKey(intercept: true).Key;
    Console.WriteLine();

    switch (actionKey)
    {
        case ConsoleKey.E:
            await selectedSim.EmptyCashBoxAsync();
            LogDemo($"Cash box emptied on {selectedPm.SerialNumber}");
            break;
        case ConsoleKey.F:
            await selectedSim.RefillPaperAsync();
            LogDemo($"Paper refilled on {selectedPm.SerialNumber}");
            break;
        case ConsoleKey.D:
            await selectedSim.CloseDoorAsync();
            LogDemo($"Door closed on {selectedPm.SerialNumber}");
            break;
        default:
            LogWarning("Invalid action.");
            break;
    }
}

// parkmeters indexes 9 at the moment (not prod)
int? GetParkmeterIndex(ConsoleKey key)
{
    var index = key switch
    {
        ConsoleKey.D1 or ConsoleKey.NumPad1 => 0,
        ConsoleKey.D2 or ConsoleKey.NumPad2 => 1,
        ConsoleKey.D3 or ConsoleKey.NumPad3 => 2,
        ConsoleKey.D4 or ConsoleKey.NumPad4 => 3,
        ConsoleKey.D5 or ConsoleKey.NumPad5 => 4,
        ConsoleKey.D6 or ConsoleKey.NumPad6 => 5,
        ConsoleKey.D7 or ConsoleKey.NumPad7 => 6,
        ConsoleKey.D8 or ConsoleKey.NumPad8 => 7,
        ConsoleKey.D9 or ConsoleKey.NumPad9 => 8,
        _ => -1 
    };

    return index >= 0 ? index : null;
}

// menus affich
void PrintDemoMenu(
    List<ParkMeterInfo> pms,
    int running,
    Dictionary<Guid, ParkMeterSimulator> sims)
{
    var activeCount = sims.Values.Count(s => !s.IsOffline);

    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine("\n────────────────────────────────────────");
    Console.WriteLine("         PARKMETER SIMULATOR");
    Console.WriteLine("────────────────────────────────────────");
    Console.WriteLine($"  Running: {activeCount}/{pms.Count} parkmeters");
    Console.WriteLine("────────────────────────────────────────");

    for (int i = 0; i < pms.Count; i++)
    {
        string indicator;
        if (!sims.ContainsKey(pms[i].Id))
            indicator = "OFF";
        else if (sims[pms[i].Id].IsOffline)
            indicator = "OFFLINE";
        else
            indicator = "ON";

        Console.WriteLine($"  [{i + 1}] {pms[i].SerialNumber} {indicator}");
    }

    Console.ForegroundColor = ConsoleColor.Gray;
    Console.WriteLine("────────────────────────────────────────");
    Console.WriteLine("  [+] Power ON next parkmeter");
    Console.WriteLine("  [-] Power OFF last parkmeter");

    if (activeCount > 0)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("──────────────────────────────────────────────────────────────");
        Console.WriteLine("  [S] Status   [G] Agent   [T] Test   [M] Menu   [Q] Quit");
        Console.WriteLine("──────────────────────────────────────────────────────────────");
    }
    Console.ResetColor();
}

void PrintTestSourceMenu(
    List<ParkMeterInfo> pms,
    Dictionary<Guid, ParkMeterSimulator> sims)
{
    var runningPms = pms.Where(p => sims.ContainsKey(p.Id)).ToList();

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("────────────────────────────────────────");
    Console.WriteLine("  Select parkmeter(s) to send alerts:");
    Console.WriteLine("  [A] ALL running parkmeters");
    for (int i = 0; i < runningPms.Count; i++)
        Console.WriteLine($"  [{i + 1}] {runningPms[i].SerialNumber}");
    Console.WriteLine("────────────────────────────────────────");
    Console.ResetColor();
}


async Task PrintSimulatorStatus(
    List<ParkMeterInfo> pms,
    Dictionary<Guid, ParkMeterSimulator> sims,
    int running,
    HttpClient http)
{
    Console.ForegroundColor = ConsoleColor.DarkCyan;
    Console.WriteLine("\n════════════════════════════════════════");
    Console.WriteLine("  PARKMETER STATUS");
    Console.WriteLine("════════════════════════════════════════");

    var index = 1;
    foreach (var pm in pms)
    {
        if (!sims.ContainsKey(pm.Id)) continue;
        var sim = sims[pm.Id];



        var freshPm = await FetchParkmeterByIdAsync(http, pm.Id);
        var apiStatus = freshPm?.Status ?? sim.Status;

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"\n  [{index++}] {pm.SerialNumber}");
        Console.ResetColor();

        Console.ForegroundColor = apiStatus == "IN_SERVICE"
            ? ConsoleColor.Green : ConsoleColor.Yellow;
        Console.WriteLine($"      Status:   {apiStatus}");
        Console.ResetColor();

        Console.ForegroundColor = sim.HasConfig
            ? ConsoleColor.Green : ConsoleColor.Red;
        Console.WriteLine($"      Config:   {(sim.HasConfig ? "yes" : "none")}");
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine($"      Cash:     {Math.Min(sim.CashLevel, 100)}%");
        Console.WriteLine($"      Paper:    {Math.Max(sim.PaperLevel, 0)}%");
        Console.ResetColor();

        var flags = new List<string>();
        if (sim.CashBoxAlertSent) flags.Add("CASH_BOX_FULL");
        if (sim.CashBoxWarnSent && !sim.CashBoxAlertSent) flags.Add("CASH_BOX_WARN");
        if (sim.PaperEmptyAlertSent) flags.Add("PAPER_EMPTY");
        if (sim.PaperAlertSent && !sim.PaperEmptyAlertSent) flags.Add("PAPER_LOW");
        if (sim.HardwareFailure) flags.Add("HARDWARE_FAILURE");
        if (sim.DoorOpen) flags.Add("DOOR_OPEN");
        if (sim.IsOffline) flags.Add("OFFLINE");

        if (flags.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Flags:    {string.Join(" | ", flags)}");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Flags: none");
        }
        Console.ResetColor();
    }

    Console.ForegroundColor = ConsoleColor.DarkCyan;
    Console.WriteLine("\n════════════════════════════════════════");
    Console.ResetColor();
}

async Task<List<string>> FetchActiveAlertsAsync(
    HttpClient http, Guid parkmeterID)
{
    var response = await http.GetAsync(
        $"api/alerts/parkmeter/{parkmeterID}");
    if (!response.IsSuccessStatusCode) return new List<string>();

    var json = await response.Content.ReadAsStringAsync();
    var alerts = JsonSerializer.Deserialize<List<AlertInfo>>(
        json, jsonOptions) ?? new List<AlertInfo>();

    return alerts
        .Where(a => a.Status == "NOT_RESOLVED")
        .Select(a => a.Type)
        .ToList();
}

// logging
void LogDemo(string message)
{
    Console.ForegroundColor = ConsoleColor.Magenta;
    Console.WriteLine($"{message}");
    Console.ResetColor();
}


void LogWarning(string message)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"[WARNING] {message}");
    Console.ResetColor();
}


// class models 

class ParkMeterInfo
{
    public Guid Id { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}


class ParkMeterConfigInfo
{
    public Guid ParkmeterID { get; set; }
    public decimal PricePerHour { get; set; }
    public int MaxDuration { get; set; } 
}


class AlertInfo
{
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}


