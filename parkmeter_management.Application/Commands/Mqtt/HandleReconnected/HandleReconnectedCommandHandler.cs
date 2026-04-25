using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Domain.Enums;
using System.Text.Json;

namespace parkmeter_management.Application.Commands.Mqtt.HandleReconnected
{
    public class HandleReconnectedCommandHandler
        : IRequestHandler<HandleReconnectedCommand, bool>
    {
        private readonly IAlertRepository _alertRepository;

        public HandleReconnectedCommandHandler(
            IAlertRepository alertRepository)
        {
            _alertRepository = alertRepository;
        }

        public async Task<bool> Handle(
            HandleReconnectedCommand command,
            CancellationToken ct)
        {
            var raw = JsonSerializer
                .Deserialize<JsonElement>(command.Payload);
            var parkmeterID = Guid.Parse(
                raw.GetProperty("ParkmeterId").GetString()!);

            // auto-resolve CONNECTION_LOST
            var alerts = await _alertRepository
                .GetByParkmeterAsync(parkmeterID);
            foreach (var a in alerts.Where(a =>
                a.Status == AlertStatus.NOT_RESOLVED &&
                a.Type == AlertType.CONNECTION_LOST))
            {
                a.Status = AlertStatus.RESOLVED;
                a.ResolvedAt = DateTime.UtcNow;
                await _alertRepository.UpdateAsync(a);
                Console.WriteLine(
                    "Auto-resolved on reconnect: CONNECTION_LOST");
            }

            return true;
        }
    }
}