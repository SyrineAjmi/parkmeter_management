using parkmeter_management.Application.Commands.MachineEvents.HandleAlerts;
using parkmeter_management.Application.Commands.MachineEvents.HandleMachineEvent;
using parkmeter_management.Application.Commands.MachineEvents.HandleStatus;
using parkmeter_management.Application.Commands.MachineEvents.HandleTransactions;
using parkmeter_management.Application.Commands.MachineEvents.HandleTelemetry;
using parkmeter_management.Domain.Enums;
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
                AlertType = raw.GetProperty("AlertType").GetInt32(),
                CreatedAt = raw.TryGetProperty("CreatedAt", out var ts)
                    ? DateTime.Parse(ts.GetString()!)
                    : DateTime.UtcNow
            };
        }

        public static HandleTransactionCommand MapTransaction(string payload)
        {
            var raw = JsonSerializer.Deserialize<JsonElement>(payload);

            return new HandleTransactionCommand
            {
                ParkmeterId = Guid.Parse(
                    raw.GetProperty("ParkmeterId").GetString()!),
                Amount = raw.GetProperty("Amount").GetDecimal(),
                Duration = raw.GetProperty("Duration").GetInt32(),
                Status = raw.GetProperty("Status").GetString()!,
                CreatedAt = raw.TryGetProperty("CreatedAt", out var ts)
                    ? DateTime.Parse(ts.GetString()!)
                    : DateTime.UtcNow
            };
        }

        public static HandleMachineEventCommand MapMachineEvent(string payload)
        {
            var raw = JsonSerializer.Deserialize<JsonElement>(payload);

            return new HandleMachineEventCommand
            {
                ParkmeterId = Guid.Parse(
                    raw.GetProperty("ParkmeterId").GetString()!),
                EventType = raw.GetProperty("EventType").GetInt32(),
                CreatedAt = raw.TryGetProperty("CreatedAt", out var ts)
                    ? DateTime.Parse(ts.GetString()!)
                    : DateTime.UtcNow
            };
        }

        public static HandleStatusCommand MapStatus(string payload)
        {
            var raw = JsonSerializer.Deserialize<JsonElement>(
                payload,
                new JsonSerializerOptions
                { PropertyNameCaseInsensitive = true });

            var statusStr = raw.GetProperty("Status").GetString()!;
            Enum.TryParse<MeterStatus>(statusStr, out var status);

            return new HandleStatusCommand
            {
                ParkmeterId = Guid.Parse(
                    raw.GetProperty("ParkmeterId").GetString()!),
                Status = status,
                CreatedAt = raw.TryGetProperty("CreatedAt", out var ts)
                    ? DateTime.Parse(ts.GetString()!)
                    : DateTime.UtcNow
            };
        }

        public static HandleTelemetryCommand MapTelemetry(string payload)
        {
            var raw = JsonSerializer.Deserialize<JsonElement>(
                payload,
                new JsonSerializerOptions
                { PropertyNameCaseInsensitive = true });

            return new HandleTelemetryCommand
            {
                ParkmeterId = Guid.Parse(
                    raw.GetProperty("ParkmeterId").GetString()!),
                CashAmount = raw.GetProperty("CashAmount").GetDecimal(),
                TransactionCount = raw.TryGetProperty(
                    "TransactionCount", out var tc)
                    ? tc.GetInt32() : 0,
                CreatedAt = raw.TryGetProperty("CreatedAt", out var ts)
                    ? DateTime.Parse(ts.GetString()!)
                    : DateTime.UtcNow
            };
        }
    }
}
