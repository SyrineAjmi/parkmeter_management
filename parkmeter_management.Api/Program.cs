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



// DATABASE
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));


// JWT
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


// SERVICES 
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


// allowing cors for ui
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowUI", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:3000",  // React
                "http://localhost:4200")  // Angular
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); 
    });
});




// MEDIATR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(
        typeof(CreateAlertCommand
        
         
        ).Assembly));


// MQTT 
builder.Services.AddSingleton<MqttBrokerService>();
builder.Services.AddSingleton<MqttSubscriberService>();
builder.Services.AddSingleton<IMqttPublisherService, MqttPublisherService>(); 
builder.Services.AddHostedService<MqttBackgroundService>();

var app = builder.Build();


// DATABASE INIT 
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    //await db.Database.EnsureDeletedAsync();  // drops everything 
    await db.Database.EnsureCreatedAsync();
    Console.WriteLine(" Database ready");

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
app.MapControllers();
app.MapHub<ParkMeterHub>("/hubs/parkmeter");



app.Run(); 
