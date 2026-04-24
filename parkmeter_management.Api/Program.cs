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
using parkmeter_management.Infrastructure.Auth;
using parkmeter_management.Infrastructure.Mqtt;
using parkmeter_management.Infrastructure.Persistence;
using parkmeter_management.Infrastructure.Persistence.Repositories;
using parkmeter_management.Infrastructure.SignalR;
using System.Text;



var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// signalR
builder.Services.AddSignalR();



// validation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();



// db
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));


// jwt
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme =
        JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme =
        JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters =
        new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    builder.Configuration["Jwt:Key"]!))
        };
});

builder.Services.AddAuthorization();


// services 
builder.Services.AddScoped<IAlertRepository, AlertRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IParkmeterConfigurationRepository, ParkmeterConfigurationRepository>();
builder.Services.AddScoped<IAlertManager, AlertManager>();
builder.Services.AddScoped<ITransactionManager, TransactionManager>();
builder.Services.AddScoped<IParkmeterConfigurationManager, ParkmeterConfigurationManager>();
builder.Services.AddScoped<IParkmeterRepository, ParkmeterRepository>();
builder.Services.AddScoped<IParkmeterManager, ParkmeterManager>();
builder.Services.AddScoped<ISessionRepository, SessionRepository>();
builder.Services.AddScoped<ISessionManager, SessionManager>();
builder.Services.AddScoped<ISiteRepository, SiteRepository>();
builder.Services.AddScoped<ISiteManager, SiteManager>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserManager, UserManager>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthManager, AuthManager>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<INotificationService,NotificationService>();
builder.Services.AddScoped<IInterventionOrderRepository, InterventionOrderRepository>();
builder.Services.AddScoped<IInterventionOrderManager, InterventionOrderManager>();
builder.Services.AddScoped<IParkMeterEventRepository, ParkMeterEventRepository>();
builder.Services.AddScoped<IEventManager, EventManager>();


// allowing cors for ui
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowUI", policy =>
    {
        policy
            .WithOrigins(
                        "http://localhost:3000",
                        "http://localhost:4200",
                        "null")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); 
    });
});




// mediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(
        typeof(CreateAlertCommand
        
         
        ).Assembly));


// mqtt 
builder.Services.AddSingleton<MqttBrokerService>();
builder.Services.AddSingleton<MqttSubscriberService>();
builder.Services.AddSingleton<IMqttPublisherService, MqttPublisherService>(); 
builder.Services.AddHostedService<MqttBackgroundService>();

var app = builder.Build();


// db initialisation 
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    //await db.Database.EnsureDeletedAsync();  // refaire la bd 
    await db.Database.EnsureCreatedAsync();
    Console.WriteLine(" Database ready");

    //// In Program.cs after app.Build():
    using (scope)
    {
        var context = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        await context.Database.MigrateAsync();

        if (!context.Users.Any())
        {
            // ── Users ──────────────────────────────
            var agent = new User
            {
                Id = Guid.NewGuid(),
                Name = "Agent",
                Email = "agent@parkmeter.tn",
                PasswordHash = BCrypt.Net.BCrypt
                    .HashPassword("Agent123!"),
                Role = parkmeter_management.Domain.Enums.Role.AGENT_INSTALLATION,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var superviseur = new User
            {
                Id = Guid.NewGuid(),
                Name = "Superviseur",
                Email = "sup@parkmeter.tn",
                PasswordHash = BCrypt.Net.BCrypt
                    .HashPassword("Sup123!"),
                Role = parkmeter_management.Domain.Enums.Role.SUPERVISEUR,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var gestionnaire = new User
            {
                Id = Guid.NewGuid(),
                Name = "Gestionnaire",
                Email = "gest@parkmeter.tn",
                PasswordHash = BCrypt.Net.BCrypt
                    .HashPassword("Gest123!"),
                Role = parkmeter_management.Domain.Enums.Role.GESTIONNAIRE_SYSTEME,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var financier = new User
            {
                Id = Guid.NewGuid(),
                Name = "Financier",
                Email = "fin@parkmeter.tn",
                PasswordHash = BCrypt.Net.BCrypt
                    .HashPassword("Fin123!"),
                Role = parkmeter_management.Domain.Enums.Role.RESPONSABLE_FINANCIER,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var operateur = new User
            {
                Id = Guid.NewGuid(),
                Name = "Operateur",
                Email = "op@parkmeter.tn",
                PasswordHash = BCrypt.Net.BCrypt
                    .HashPassword("Op123!"),
                Role = parkmeter_management.Domain.Enums.Role.OPERATEUR,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await context.Users.AddRangeAsync(
                agent, superviseur, gestionnaire,
                financier, operateur);
            await context.SaveChangesAsync();
            Console.WriteLine("✅ Users seeded");

            // ── Sites ──────────────────────────────
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


            // ── Parkmeters ─────────────────────────
            var pm1 = new Parkmeter
            {
                Id = Guid.NewGuid(),
                SerialNumber = "PM001",
                Status = parkmeter_management.Domain.Enums.MeterStatus.OUT_OF_SERVICE,
                SiteId = site1.Id,
                CreatedAt = DateTime.UtcNow
            };

            var pm2 = new Parkmeter
            {
                Id = Guid.NewGuid(),
                SerialNumber = "PM002",
                Status = parkmeter_management.Domain.Enums.MeterStatus.OUT_OF_SERVICE,
                SiteId = site2.Id,
                CreatedAt = DateTime.UtcNow
            };

            await context.Parkmeters.AddRangeAsync(pm1, pm2);
            await context.SaveChangesAsync();
            Console.WriteLine("✅ Parkmeters seeded");

            // ── Configurations ─────────────────────
            var config1 = new ParkMeterConfig
            {
                Id = Guid.NewGuid(),
                ParkmeterId = pm1.Id,
                PricePerHour = 1.00m,
                MaxDuration = 120,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var config2 = new ParkMeterConfig
            {
                Id = Guid.NewGuid(),
                ParkmeterId = pm2.Id,
                PricePerHour = 1.50m,
                MaxDuration = 90,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await context.Configurations
                .AddRangeAsync(config1, config2);
            await context.SaveChangesAsync();
            Console.WriteLine("✅ Configurations seeded");

        }
    }

}



if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseCors("AllowUI");   
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<ActiveUserMiddleware>(); 
app.MapControllers();
app.MapHub<ParkMeterHub>("/hubs/parkmeter");



app.Run(); 
