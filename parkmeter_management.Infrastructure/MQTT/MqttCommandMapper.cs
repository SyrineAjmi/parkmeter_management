using parkmeter_management.Application.Commands.MachineEvents.HandleAlerts;
using parkmeter_management.Application.Commands.MachineEvents.HandleMachineEvent;
using parkmeter_management.Application.Commands.MachineEvents.HandleTransactions;
using System.Text.Json;

namespace parkmeter_management.Infrastructure.Mqtt
{
    // translating transport payload to domain commands
    public static class MqttCommandMapper
    {
        public static HandleAlertCommand MapAlert(string payload)
        {
            var raw = JsonSerializer.Deserialize<JsonElement>(payload);

            return new HandleAlertCommand
            {
                ParkmeterId = Guid.Parse(
                    raw.GetProperty("ParkmeterId").GetString()!),
                AlertType = raw
                    .GetProperty("AlertType").GetInt32(),
                CreatedAt = raw.TryGetProperty(
                    "CreatedAt", out var ts)
                    ? DateTime.Parse(ts.GetString()!)
                    : DateTime.UtcNow
            };
        }

        public static HandleTransactionCommand MapTransaction(
            string payload)
        {
            var raw = JsonSerializer.Deserialize<JsonElement>(payload);

            return new HandleTransactionCommand
            {
                ParkmeterId = Guid.Parse(
                    raw.GetProperty("ParkmeterId").GetString()!),
                Amount = raw
                    .GetProperty("Amount").GetDecimal(),
                Duration = raw
                    .GetProperty("Duration").GetInt32(),
                Status = raw
                    .GetProperty("Status").GetString()!,
                CreatedAt = raw.TryGetProperty(
                    "CreatedAt", out var ts)
                    ? DateTime.Parse(ts.GetString()!)
                    : DateTime.UtcNow
            };
        }

        public static HandleMachineEventCommand MapMachineEvent(
            string payload)
        {
            var raw = JsonSerializer.Deserialize<JsonElement>(payload);

            return new HandleMachineEventCommand
            {
                ParkmeterId = Guid.Parse(
                    raw.GetProperty("ParkmeterId").GetString()!),
                EventType = raw
                    .GetProperty("EventType").GetInt32(),
                CreatedAt = raw.TryGetProperty(
                    "CreatedAt", out var ts)
                    ? DateTime.Parse(ts.GetString()!)
                    : DateTime.UtcNow
            };
        }
    }
}