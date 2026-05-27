using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using parkmeter_management.Api.BackgroundServices;
using parkmeter_management.Api.Middleware;
using parkmeter_management.Application.Commands.Alerts;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Application.Interfaces.Services;
using parkmeter_management.Application.Managers.Implementations;
using parkmeter_management.Application.Managers.Interfaces;
using parkmeter_management.Domain.Entities;
using parkmeter_management.Domain.Enums;
using parkmeter_management.Infrastructure.Auth;
using parkmeter_management.Infrastructure.Mqtt;
using parkmeter_management.Infrastructure.Persistence;
using parkmeter_management.Infrastructure.Persistence.Repositories;
using parkmeter_management.Infrastructure.SignalR;
using System.Text;


// force console to flush on every write
var stdout = new StreamWriter(Console.OpenStandardOutput())
{
    AutoFlush = true
};
Console.SetOut(stdout);
Console.OutputEncoding = System.Text.Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);

// ── Controllers ───────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
        opts.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── SignalR ───────────────────────────────────
builder.Services.AddSignalR();

// ── Validation ────────────────────────────────
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// ── Database ──────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// ── JWT authentication ────────────────────────
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/hubs/parkmeter"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// ── Repositories ──────────────────────────────
builder.Services.AddScoped<IAlertRepository, AlertRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IParkmeterConfigurationRepository, ParkmeterConfigurationRepository>();
builder.Services.AddScoped<IParkmeterRepository, ParkmeterRepository>();
builder.Services.AddScoped<ISessionRepository, SessionRepository>();
builder.Services.AddScoped<ISiteRepository, SiteRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IInterventionOrderRepository, InterventionOrderRepository>();

// ── Managers ──────────────────────────────────
builder.Services.AddScoped<IAlertManager, AlertManager>();
builder.Services.AddScoped<ITransactionManager, TransactionManager>();
builder.Services.AddScoped<IParkmeterConfigurationManager, ParkmeterConfigurationManager>();
builder.Services.AddScoped<IParkmeterManager, ParkmeterManager>();
builder.Services.AddScoped<ISessionManager, SessionManager>();
builder.Services.AddScoped<ISiteManager, SiteManager>();
builder.Services.AddScoped<IUserManager, UserManager>();
builder.Services.AddScoped<IAuthManager, AuthManager>();
builder.Services.AddScoped<IInterventionOrderManager, InterventionOrderManager>();
builder.Services.AddScoped<IMachineEventRepository, MachineEventRepository>();
builder.Services.AddScoped<IMachineEventManager, MachineEventManager>();


// ── Services ──────────────────────────────────
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// ── MediatR ───────────────────────────────────
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(CreateAlertCommand).Assembly));

// ── MQTT ──────────────────────────────────────────────────────
builder.Services.AddSingleton<MqttBrokerService>();
builder.Services.AddSingleton<MqttClientService>();
builder.Services.AddSingleton<IMessagePublisherService>(
    sp => sp.GetRequiredService<MqttClientService>());
builder.Services.AddHostedService<MqttBackgroundService>();

// ── CORS ──────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy
            //.WithOrigins("http://localhost:3000", "http://localhost:4200", "http://localhost:3001")
            .SetIsOriginAllowed(_ => true)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

// ─────────────────────────────────────────────
var app = builder.Build();


// ── Database init ─────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider
        .GetRequiredService<AppDbContext>();
    //await context.Database.EnsureDeletedAsync();

    try
    {
        await context.Database.MigrateAsync();
        Console.WriteLine("Database ready");
    }
    catch (Exception ex) when (ex.Message.Contains("already exists"))
    {
        Console.WriteLine("Database already exists — skipping creation");
    }

    // if DB empty
    if (!context.Users.Any())
    {
        // ── Users ──────────────────────────────────────
        var agent = new User
        {
            Id = Guid.NewGuid(),
            Name = "Agent",
            Email = "agent@parkmeter.tn",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Agent123!"),
            Role = Role.AGENT_INSTALLATION,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var superviseur = new User
        {
            Id = Guid.NewGuid(),
            Name = "Superviseur",
            Email = "sup@parkmeter.tn",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Sup123!"),
            Role = Role.SUPERVISEUR,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var gestionnaire = new User
        {
            Id = Guid.NewGuid(),
            Name = "Gestionnaire",
            Email = "gest@parkmeter.tn",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Gest123!"),
            Role = Role.GESTIONNAIRE_SYSTEME,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var financier = new User
        {
            Id = Guid.NewGuid(),
            Name = "Financier",
            Email = "fin@parkmeter.tn",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Fin123!"),
            Role = Role.RESPONSABLE_FINANCIER,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var operateur = new User
        {
            Id = Guid.NewGuid(),
            Name = "Operateur",
            Email = "op@parkmeter.tn",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Op123!"),
            Role = Role.OPERATEUR,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await context.Users.AddRangeAsync(
            agent, superviseur, gestionnaire,
            financier, operateur);
        await context.SaveChangesAsync();
        Console.WriteLine("✅ Users seeded");

        // ── Sites ───────────────────────────────────────
        var site1 = new Site
        {
            Id = Guid.NewGuid(),
            Name = "Centre Ville",
            Address = "Rue de la Republique, Tunis",
            Description = "Parking centre ville",
            CreatedAt = DateTime.UtcNow
        };

        var site2 = new Site
        {
            Id = Guid.NewGuid(),
            Name = "La Marsa",
            Address = "Avenue Habib Bourguiba, La Marsa",
            Description = "Parking bord de mer",
            CreatedAt = DateTime.UtcNow
        };

        await context.Sites.AddRangeAsync(site1, site2);
        await context.SaveChangesAsync();
        Console.WriteLine("✅ Sites seeded");

        // ── Parkmeters ──────────────────────────────────
        var pm1 = new Parkmeter
        {
            Id = Guid.NewGuid(),
            SerialNumber = "PM001",
            Status = MeterStatus.PENDING_ACTIVATION,
            SiteId = site1.Id,
            CreatedAt = DateTime.UtcNow
        };

        var pm2 = new Parkmeter
        {
            Id = Guid.NewGuid(),
            SerialNumber = "PM002",
            Status = MeterStatus.PENDING_ACTIVATION,
            SiteId = site1.Id,
            CreatedAt = DateTime.UtcNow
        };

        var pm3 = new Parkmeter
        {
            Id = Guid.NewGuid(),
            SerialNumber = "PM003",
            Status = MeterStatus.PENDING_ACTIVATION,
            SiteId = site2.Id,
            CreatedAt = DateTime.UtcNow
        };

        var pm4 = new Parkmeter
        {
            Id = Guid.NewGuid(),
            SerialNumber = "PM004",
            Status = MeterStatus.PENDING_ACTIVATION,
            SiteId = site2.Id,
            CreatedAt = DateTime.UtcNow
        };

        await context.Parkmeters.AddRangeAsync(pm1, pm2, pm3, pm4);
        await context.SaveChangesAsync();
        Console.WriteLine("✅ Parkmeters seeded");

        // ── Configurations ──────────────────────────────
        var config1 = new ParkMeterConfig
        {
            Id = Guid.NewGuid(),
            ParkmeterId = pm1.Id,
            MaxDuration = 120,
            StrategyType = PricingStrategyType.FLAT_RATE,
            PricePerHour = 1.000m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var config2 = new ParkMeterConfig
        {
            Id = Guid.NewGuid(),
            ParkmeterId = pm2.Id,
            MaxDuration = 120,
            StrategyType = PricingStrategyType.TIME_OF_DAY,
            PeakRate = 2.500m,
            OffPeakRate = 1.000m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var config3 = new ParkMeterConfig
        {
            Id = Guid.NewGuid(),
            ParkmeterId = pm3.Id,
            MaxDuration = 90,
            StrategyType = PricingStrategyType.WEEKDAY_WEEKEND,
            WeekdayRate = 1.500m,
            WeekendRate = 0.500m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await context.Configurations
            .AddRangeAsync(config1, config2, config3);
        await context.SaveChangesAsync();
        Console.WriteLine(" Configurations seeded");

        // ── Sessions ────────────────────────────────────
        var session1 = new Session
        {
            Id = Guid.NewGuid(),
            ParkmeterId = pm1.Id,
            StartDateTime = DateTime.UtcNow,
            Status = SessionStatus.ACTIVE,
            TotalTransactions = 0,
            TotalAmount = 0
        };
        await context.Sessions
            .AddRangeAsync(session1);
        await context.SaveChangesAsync();
        Console.WriteLine(" Sessions seeded");

        Console.WriteLine(" Database seeded successfully");
    }
    else
    {
        Console.WriteLine(" Database already seeded — skipping");
    }
}



// ── Middleware pipeline ───────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<ActiveUserMiddleware>();
app.MapGet("/health", () => Results.Ok("ready"));
app.MapControllers();
app.MapHub<ParkMeterHub>("/hubs/parkmeter");

app.Run();