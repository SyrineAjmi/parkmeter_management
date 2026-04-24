using Microsoft.AspNetCore.SignalR;
using parkmeter_management.Application.Interfaces.Services;

namespace parkmeter_management.Infrastructure.SignalR
{
    public class NotificationService : INotificationService
    {
        private readonly IHubContext<ParkMeterHub> _hubContext;

        public NotificationService(IHubContext<ParkMeterHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task NotifyNewAlert(
            Guid parkmeterID, string alertType, string priority)
        {
            await _hubContext.Clients.All.SendAsync(
                "NewAlert", new
                {
                    ParkmeterID = parkmeterID,
                    AlertType = alertType,
                    Priority = priority,
                    Timestamp = DateTime.UtcNow
                });
        }

        public async Task NotifyAlertResolved(
            Guid alertId, Guid parkmeterID)
        {
            await _hubContext.Clients.All.SendAsync(
                "AlertResolved", new
                {
                    AlertId = alertId,
                    ParkmeterID = parkmeterID,
                    Timestamp = DateTime.UtcNow
                });
        }

        public async Task NotifyStatusChanged(
            Guid parkmeterID, string newStatus)
        {
            await _hubContext.Clients.All.SendAsync(
                "StatusChanged", new
                {
                    ParkmeterID = parkmeterID,
                    Status = newStatus,
                    Timestamp = DateTime.UtcNow
                });
        }

        public async Task NotifyNewTransaction(
            Guid parkmeterID, decimal amount, string status)
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
            Guid parkmeterID, decimal totalAmount, int totalTransactions)
        {
            await _hubContext.Clients.All.SendAsync(
                "SessionClosed", new
                {
                    ParkmeterID = parkmeterID,
                    TotalAmount = totalAmount,
                    TotalTransactions = totalTransactions,
                    Timestamp = DateTime.UtcNow
                });
        }

        public async Task NotifyInterventionOrderCreated(
            Guid operatorId, Guid parkmeterID, Guid orderId)
        {
            await _hubContext.Clients.All.SendAsync(
                "InterventionOrderCreated", new
                {
                    OperatorId = operatorId,
                    ParkmeterID = parkmeterID,
                    OrderId = orderId,
                    Timestamp = DateTime.UtcNow
                });
        }

        public async Task NotifyInterventionOrderCompleted(
            Guid orderId, Guid parkmeterID, DateTime completedAt)
        {
            await _hubContext.Clients.All.SendAsync(
                "InterventionOrderCompleted", new
                {
                    OrderId = orderId,
                    ParkmeterID = parkmeterID,
                    CompletedAt = completedAt
                });
        }

        // NEW — notifies frontend when a machine event is recorded
        public async Task NotifyNewEvent(
            Guid parkmeterID, string eventType)
        {
            await _hubContext.Clients.All.SendAsync(
                "NewEvent", new
                {
                    ParkmeterID = parkmeterID,
                    EventType = eventType,
                    Timestamp = DateTime.UtcNow
                });
        }
    }
}