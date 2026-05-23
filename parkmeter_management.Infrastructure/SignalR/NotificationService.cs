using Microsoft.AspNetCore.SignalR;
using parkmeter_management.Application.Interfaces.Services;

namespace parkmeter_management.Infrastructure.SignalR
{
    public class NotificationService : INotificationService
    {
        private readonly IHubContext<ParkMeterHub> _hubContext;

        public NotificationService(
            IHubContext<ParkMeterHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task NotifyNewAlert(
            Guid parkmeterID,
            string serialNumber,
            string alertType,
            string priority)
        {
            await _hubContext.Clients.All.SendAsync(
                "NewAlert", new
                {
                    ParkmeterID = parkmeterID,
                    SerialNumber = serialNumber,
                    AlertType = alertType,
                    Priority = priority,
                    Timestamp = DateTime.UtcNow
                });
        }

        public async Task NotifyAlertResolved(
            Guid alertId,
            string serialNumber,
            Guid parkmeterID)
        {
            await _hubContext.Clients.All.SendAsync(
                "AlertResolved", new
                {
                    AlertId = alertId,
                    SerialNumber = serialNumber,
                    ParkmeterID = parkmeterID,
                    Timestamp = DateTime.UtcNow
                });
        }

        public async Task NotifyParkmeterOnline(Guid parkmeterID, string serialNumber)
        {
            await _hubContext.Clients.All.SendAsync(
                "ParkmeterOnline", new
                {
                    ParkmeterID = parkmeterID,
                    SerialNumber = serialNumber,
                    Timestamp = DateTime.UtcNow
                });
        }

        public async Task NotifyStatusChanged(
            Guid parkmeterID,
            string serialNumber,
            string newStatus)
        {
            await _hubContext.Clients.All.SendAsync(
                "StatusChanged", new
                {
                    ParkmeterID = parkmeterID,
                    SerialNumber = serialNumber,
                    Status = newStatus,
                    Timestamp = DateTime.UtcNow
                });
        }

        public async Task NotifyNewTransaction(
            Guid parkmeterID,
            decimal amount,
            string status)
        {
            await _hubContext.Clients.All.SendAsync(
                "NewTransaction", new
                {
                    ParkmeterID = parkmeterID,
                    Amount = amount,
                    Status = status,
                    Timestamp = DateTime.UtcNow
                });
        }

        public async Task NotifySessionClosed(
            Guid parkmeterID,
            string serialNumber,
            decimal totalAmount,
            int totalTransactions)
        {
            await _hubContext.Clients.All.SendAsync(
                "SessionClosed", new
                {
                    ParkmeterID = parkmeterID,
                    SerialNumber = serialNumber,
                    TotalAmount = totalAmount,
                    TotalTransactions = totalTransactions,
                    Timestamp = DateTime.UtcNow
                });
        }

        public async Task NotifyInterventionOrderCreated(
            Guid operatorId,
            string serialNumber,
            Guid parkmeterID,
            Guid orderId)
        {
            await _hubContext.Clients.All.SendAsync(
                "InterventionOrderCreated", new
                {
                    OperatorId = operatorId,
                    SerialNumber = serialNumber,
                    ParkmeterID = parkmeterID,
                    OrderId = orderId,
                    Timestamp = DateTime.UtcNow
                });
        }

        public async Task NotifyInterventionOrderCompleted(
            Guid orderId,
            string serialNumber,
            Guid parkmeterID,
            DateTime completedAt)
        {
            await _hubContext.Clients.All.SendAsync(
                "InterventionOrderCompleted", new
                {
                    OrderId = orderId,
                    SerialNumber = serialNumber,
                    ParkmeterID = parkmeterID,
                    CompletedAt = completedAt
                });
        }

        public async Task NotifyTelemetry(
            Guid parkmeterID,
            decimal cashAmount,
            int transactionCount)
        {
            await _hubContext.Clients.All.SendAsync(
                "Telemetry", new
                {
                    ParkmeterID = parkmeterID,
                    CashAmount = cashAmount,
                    TransactionCount = transactionCount,
                    Timestamp = DateTime.UtcNow
                });
        }

    }
}