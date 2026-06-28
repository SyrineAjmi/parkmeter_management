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
using parkmeter_management.Infrastructure.AI;
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

// ── AI ────────────────────────────────────────────────────────
builder.Services.AddHttpClient("Gemini", client =>
{
    client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
});
builder.Services.AddScoped<IAiReportService, AiReportService>();


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
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();



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
        var agent = new User("Agent",
            "agent@parkmeter.tn",
            BCrypt.Net.BCrypt.HashPassword("Agent123!"),
            Role.AGENT_INSTALLATION);

        var superviseur = new User("Superviseur",
                          "sup@parkmeter.tn",
                          BCrypt.Net.BCrypt.HashPassword("Sup123!"),
                          Role.SUPERVISEUR);

        var gestionnaire = new User("Gestionnaire",
                            "gest@parkmeter.tn",
                            BCrypt.Net.BCrypt.HashPassword("Gest123!"),
                             Role.GESTIONNAIRE_SYSTEME);

        var financier = new User("Financier",
                            "fin@parkmeter.tn",
                            BCrypt.Net.BCrypt.HashPassword("Fin123!"),
                            Role.RESPONSABLE_FINANCIER);

        var operateur = new User("Operateur",
                        "op@parkmeter.tn",
                        BCrypt.Net.BCrypt.HashPassword("Op123!"),
                        Role.OPERATEUR);



        await context.Users.AddRangeAsync(
            agent, superviseur, gestionnaire,
            financier, operateur);
        await context.SaveChangesAsync();
        Console.WriteLine("✅ Users seeded");

        // ── Sites ───────────────────────────────────────
        var site1 = new Site(
            "Centre Ville",
            "Rue de la Republique, Tunis",
            "Parking centre ville",
            36.8190,
            10.1658
            );

        operateur.AssignToSite(site1.Id);


        var site2 = new Site
        (
            "La Marsa",
            "Avenue Habib Bourguiba, La Marsa",
            "Parking bord de mer",
            36.8781,
            10.3247
        );

        await context.Sites.AddRangeAsync(site1, site2);
        await context.SaveChangesAsync();
        Console.WriteLine("✅ Sites seeded");

        // ── Parkmeters ──────────────────────────────────
        var pm1 = new Parkmeter
        ("PM001",
          site1.Id
        );

        var pm2 = new Parkmeter
        (
              "PM002",
              site1.Id
        );

        var pm3 = new Parkmeter
        (
            "PM003",
            site2.Id
        );

        var pm4 = new Parkmeter
       ( 
            "PM004",
            site2.Id
        );

        await context.Parkmeters.AddRangeAsync(pm1, pm2, pm3, pm4);
        await context.SaveChangesAsync();
        Console.WriteLine("Parkmeters seeded");

        // ── Configurations ──────────────────────────────
        var config1 = new ParkMeterConfig
        (pm1.Id,
        120,
        PricingStrategyType.FLAT_RATE,
        1.000m, 
        null, 
        null, 
        null, 
        null
        );

        var config2 = new ParkMeterConfig(pm2.Id, 120, PricingStrategyType.TIME_OF_DAY, null, 2.500m, 1.000m, null, null);


        var config3 = new ParkMeterConfig(pm3.Id, 90, PricingStrategyType.WEEKDAY_WEEKEND, null, null, null, 1.500m, 0.500m);


        await context.Configurations
            .AddRangeAsync(config1, config2, config3);
        await context.SaveChangesAsync();
        Console.WriteLine(" Configurations seeded");

        // ── Sessions ────────────────────────────────────
        var session1 = new Session(pm1.Id);

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