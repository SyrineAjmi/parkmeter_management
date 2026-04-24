using Microsoft.EntityFrameworkCore;
using parkmeter_management.Application.Commands.Simulator.HandleCashRemoved;
using parkmeter_management.Application
    .Interfaces.Repositories;
using parkmeter_management.Application
    .Interfaces.Services;
using parkmeter_management.Infrastructure.Mqtt;
using parkmeter_management.Infrastructure.Persistence;
using parkmeter_management.Infrastructure
    .Persistence.Repositories;
using parkmeter_management.Infrastructure.SignalR;
using parkmeter_management.SimulatorApplication
    .Commands.HandleCashRemoved;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration
            .GetConnectionString("DefaultConnection")));

// ── Repositories ──────────────────────────────
builder.Services.AddScoped<IAlertRepository, AlertRepository>();
builder.Services.AddScoped<IParkmeterRepository, ParkmeterRepository>();
builder.Services.AddScoped<ISessionRepository, SessionRepository>();
builder.Services.AddScoped<IInterventionOrderRepository,InterventionOrderRepository>();

// ── Services ──────────────────────────────────
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IMqttPublisherService, MqttPublisherService>();

// ── MediatR ───────────────────────────────────
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(
        typeof(HandleCashRemovedCommand).Assembly));

// ── SignalR ───────────────────────────────────
builder.Services.AddSignalR();

// ── Controllers ───────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions
            .Converters.Add(
                new System.Text.Json
                    .Serialization
                    .JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseRouting();
app.MapHub<ParkMeterHub>("/parkmeterHub");
app.MapControllers();

app.Run();