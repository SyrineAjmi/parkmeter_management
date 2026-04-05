using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using parkmeter_management.Application.Commands.Alerts;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Infrastructure.Mqtt;
using parkmeter_management.Infrastructure.Persistence;
using parkmeter_management.Infrastructure.Persistence.Repositories;

Console.WriteLine("🚀 Starting Orchestrator...");

// ── DEPENDENCY INJECTION CONTAINER ──────────────────
var services = new ServiceCollection();

// Database
services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer("Server=PC-SYRINE\\SQLEXPRESS;Database=ParkMeterDB;Trusted_Connection=True;TrustServerCertificate=True;"));

// Repositories (Dependency Inversion: bind interface → implementation)
services.AddScoped<IAlertRepository, AlertRepository>();

// MediatR (scans Application assembly for all handlers)
services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(
        typeof(CreateAlertCommandHandler).Assembly));

var serviceProvider = services.BuildServiceProvider();

// ── CREATE DATABASE IF NOT EXISTS ───────────────────
using (var scope = serviceProvider.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
    Console.WriteLine("✅ Database ready");
}

// ── START MQTT ───────────────────────────────────────
var broker = new MqttBrokerService();
await broker.StartAsync();

var subscriber = new MqttSubscriberService(serviceProvider);
await subscriber.StartAsync();

Console.WriteLine("─────────────────────────────────────");
Console.WriteLine("Waiting for messages... (Ctrl+C to stop)");

await Task.Delay(Timeout.Infinite);