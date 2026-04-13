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
var simulators = new List<ParkMeterSimulator>();
var simTasks = new List<Task>();
var cts = new CancellationTokenSource();

_ = StartNewParkmeterListenerAsync(http, parkmeters, cts.Token);
PrintDemoMenu(parkmeters, runningCount, simulators);


// ── main loop ─────────────────────────────────────────
while (true)
{
        //auto-detect offline parkmeters
        for (int i = simulators.Count - 1; i >= 0; i--)
        {
            if (simulators[i].IsOffline && !simulators[i].IsConnectionLost)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[POWER] {parkmeters[i].SerialNumber} " +
                    $"permanently offline — press [+] to restart");
                Console.ResetColor();
                simulators.RemoveAt(i);
                simTasks.RemoveAt(i);
                runningCount = simulators.Count;
                PrintDemoMenu(parkmeters, runningCount, simulators);
            }
        }

        if (!Console.KeyAvailable)
        {
            await Task.Delay(100);
            continue;
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
        PrintSimulatorStatus(parkmeters, simulators, runningCount,http);
        continue;
    }
    if (key == ConsoleKey.G)
    {
        if (simulators.Count == 0)
        { LogWarning("No parkmeters running!"); continue; }
        await RunAgentModeAsync(parkmeters, simulators, runningCount);
        PrintDemoMenu(parkmeters, runningCount, simulators);
        continue;
    }
    if (key == ConsoleKey.T)
    {
        if (simulators.Count == 0)
        { LogWarning("No parkmeters running!"); continue; }
        await RunTestModeAsync(parkmeters, simulators, runningCount);
        PrintDemoMenu(parkmeters, runningCount, simulators);
        continue;
    }
    if (key == ConsoleKey.OemPlus
     || key == ConsoleKey.Add) { await PowerOnAsync(http, parkmeters, simulators, simTasks, cts); runningCount = simulators.Count; 
        PrintDemoMenu(parkmeters, runningCount, simulators); continue; }
    if (key == ConsoleKey.OemMinus
     || key == ConsoleKey.Subtract) { await PowerOffAsync(parkmeters, simulators, simTasks); runningCount = simulators.Count; 
        PrintDemoMenu(parkmeters, runningCount,simulators); continue; }
}


// ── shutting down ──────────────────────────────────────────
async Task ShutdownAsync(
    CancellationTokenSource cts, List<Task> tasks)
{
    Console.WriteLine("\nShutting down all parkmeters...");
    cts.Cancel();
    if (tasks.Count == 0) return;
    try { await Task.WhenAll(tasks); }
    catch (OperationCanceledException)
    { Console.WriteLine("All parkmeters stopped."); }
}


// ── powering on the parkmeters ──────────────────────────────────────
async Task PowerOnAsync(
    HttpClient http,
    List<ParkMeterInfo> pms,
    List<ParkMeterSimulator> sims,
    List<Task> tasks,
    CancellationTokenSource cts)
{
    var currentCount = sims.Count;
    if (currentCount >= pms.Count)
    { LogWarning($"All {pms.Count} parkmeters already running!"); return; } 

    var pm = pms[currentCount];
    var config = await FetchConfigAsync(http, pm.Id);

    var state = new ParkMeterState
    {
        Id = pm.Id,
        SerialNumber = pm.SerialNumber,
        Status = pm.Status,
        PricePerHour = config?.PricePerHour ?? 0,
        MaxDuration = config?.MaxDuration ?? 0,
        HasConfig = config != null,
        MinTickDelayMs = 15000,
        MaxTickDelayMs = 30000,
        CashFillPerTick = 8,
        PaperUsePerTick = 3
    };

    var simulator = new ParkMeterSimulator(state);
    sims.Add(simulator);
    tasks.Add(simulator.RunAsync(cts.Token));

    LogDemo($"[+] {pm.SerialNumber} powered ON | Status: {state.Status} | " +
        $"Config: {(config != null ? $"{state.PricePerHour} DT/h | Max: {state.MaxDuration} min" : "none")}");
}


// ── powering off the parkmeters ──────────────────────────────────
async Task PowerOffAsync(
    List<ParkMeterInfo> pms,
    List<ParkMeterSimulator> sims,
    List<Task> tasks)
{
    if (sims.Count == 0)
    { LogWarning("No parkmeters running!"); return; }

    var last = sims.Count - 1;
    await sims[last].StopAsync();
    LogDemo($"[-] {pms[last].SerialNumber} powered OFF");

    sims.RemoveAt(last);
    tasks.RemoveAt(last);
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
    List<ParkMeterSimulator> sims,
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
    PrintTestSourceMenu(pms, running);
    var sourceKey = Console.ReadKey(intercept: true).Key;
    Console.WriteLine();

    List<(ParkMeterSimulator sim, string serial)> targets = new();

    if (sourceKey == ConsoleKey.A)
    {
        targets = sims.Select((s, i) => (s, pms[i].SerialNumber)).ToList();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("  → ALL parkmeters selected");
        Console.ResetColor();
    }
    else
    {
        var idx = GetParkmeterIndex(sourceKey);
        if (idx == null || idx >= sims.Count)
        { LogWarning("Invalid selection."); return; }
        targets.Add((sims[idx.Value], pms[idx.Value].SerialNumber));
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"  → {pms[idx.Value].SerialNumber} selected");
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

    //

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
            var indicator = isSelected ? "✅" : "  ";
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

// ── physical actions of the agent ─────────────────────────────────────
async Task RunAgentModeAsync(
    List<ParkMeterInfo> pms,
    List<ParkMeterSimulator> sims,
    int running)
{
    // ── Step 1: Select parkmeter ──────────────────────
    Console.ForegroundColor = ConsoleColor.DarkCyan;
    Console.WriteLine("\n════════════════════════════════════════");
    Console.WriteLine("  AGENT MODE");
    Console.WriteLine("════════════════════════════════════════");
    for (int i = 0; i < running; i++)
        Console.WriteLine($"    [{i + 1}] {pms[i].SerialNumber} " +
            $"({sims[i].Status})");
    Console.WriteLine("────────────────────────────────────────");
    Console.ResetColor();

    var pmKey = Console.ReadKey(intercept: true).Key;
    Console.WriteLine();

    var idx = GetParkmeterIndex(pmKey);
    if (idx == null || idx >= sims.Count)
    { LogWarning("Invalid selection."); return; }

    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"  → {pms[idx.Value].SerialNumber} selected");
    Console.ResetColor();

    // ── Step 2: Select action ─────────────────────────
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
            await sims[idx.Value].EmptyCashBoxAsync();
            LogDemo($"Cash box emptied on {pms[idx.Value].SerialNumber}");
            break;
        case ConsoleKey.F:
            await sims[idx.Value].RefillPaperAsync();
            LogDemo($"Paper refilled on {pms[idx.Value].SerialNumber}");
            break;
        case ConsoleKey.D:
            await sims[idx.Value].CloseDoorAsync();
            LogDemo($"Door closed on {pms[idx.Value].SerialNumber}");
            break;
        default:
            LogWarning("Invalid action.");
            break;
    }
}

// ── index mappers max 9 parkmeters at the moment ────────────────────────────────────
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

// ── menus ────────────────────────────────────────────
void PrintDemoMenu(
    List<ParkMeterInfo> pms,
    int running,
    List<ParkMeterSimulator> sims)
{
    var activeCount = sims.Count(s => !s.IsOffline);

    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine("\n────────────────────────────────────────");
    Console.WriteLine("         PARKMETER SIMULATOR");
    Console.WriteLine("────────────────────────────────────────");
    Console.WriteLine($"  Running: {activeCount}/{pms.Count} parkmeters"); // ← use activeCount
    Console.WriteLine("────────────────────────────────────────");

    for (int i = 0; i < pms.Count; i++)
    {
        string indicator;
        if (i >= sims.Count)
            indicator = "OFF";
        else if (sims[i].IsOffline)
            indicator = "OFFLINE ";
        else
            indicator = "ON";

        Console.WriteLine($"  [{i + 1}] {pms[i].SerialNumber} {indicator}");
    }

    Console.ForegroundColor = ConsoleColor.Gray;
    Console.WriteLine("────────────────────────────────────────");
    Console.WriteLine("  [+] Power ON next parkmeter ");
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

void PrintTestSourceMenu(List<ParkMeterInfo> pms, int running)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("────────────────────────────────────────");
    Console.WriteLine("  Select parkmeter(s) to send alerts:");
    Console.WriteLine("  [A] ALL running parkmeters");
    for (int i = 0; i < running; i++)
        Console.WriteLine($"  [{i + 1}] {pms[i].SerialNumber}");
    Console.WriteLine("────────────────────────────────────────");
    Console.ResetColor();
}


async Task PrintSimulatorStatus(
    List<ParkMeterInfo> pms,
    List<ParkMeterSimulator> sims,
    int running,
    HttpClient http)
{
    Console.ForegroundColor = ConsoleColor.DarkCyan;
    Console.WriteLine("\n════════════════════════════════════════");
    Console.WriteLine("  SIMULATOR STATUS");
    Console.WriteLine("════════════════════════════════════════");

    for (int i = 0; i < running; i++)
    {
        var sim = sims[i];

        // fetch active alerts from API
        var activeAlerts = await FetchActiveAlertsAsync(http, pms[i].Id);

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"\n  [{i + 1}] {pms[i].SerialNumber}");
        Console.ResetColor();

        // Status
        Console.ForegroundColor = sim.Status == "IN_SERVICE"
            ? ConsoleColor.Green : ConsoleColor.Yellow;
        Console.WriteLine($"      Status:   {sim.Status}");
        Console.ResetColor();

        // Config
        Console.ForegroundColor = sim.HasConfig
            ? ConsoleColor.Green : ConsoleColor.Red;
        Console.WriteLine($"      Config:   {(sim.HasConfig ? "yes" : "none")}");
        Console.ResetColor();

        // Levels
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine($"      Cash:     {sim.CashLevel}%");
        Console.WriteLine($"      Paper:    {sim.PaperLevel}%");
        Console.ResetColor();

        // Flags from API
        if (sim.Status == "IN_SERVICE")
            activeAlerts.Clear();

        if (sim.IsOffline) activeAlerts.Add("OFFLINE");

        if (activeAlerts.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"      Flags:    {string.Join(" | ", activeAlerts)}");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"      Flags:    ✅ none");
            Console.ResetColor();
        }
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

// ── logging ───────────────────────────────────────────
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


// ── models ──────────────────────────────────────────── 
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


