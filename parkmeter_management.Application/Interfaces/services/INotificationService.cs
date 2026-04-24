namespace parkmeter_management.Application.Interfaces.Services
{
    public interface INotificationService
    {
        Task NotifyNewAlert(
            Guid parkmeterID,
            string alertType,
            string priority);

        Task NotifyAlertResolved(
            Guid alertId,
            Guid parkmeterID);

        Task NotifyStatusChanged(
            Guid parkmeterID,
            string newStatus);

        Task NotifyNewTransaction(
            Guid parkmeterID,
            decimal amount,
            string status);

        Task NotifySessionClosed(
            Guid parkmeterID,
            decimal totalAmount,
            int totalTransactions);

        Task NotifyInterventionOrderCreated(
            Guid operatorId,
            Guid parkmeterID,
            Guid orderId);

        Task NotifyInterventionOrderCompleted(
            Guid orderId,
            Guid parkmeterID,
            DateTime completedAt);

        // notifies the frontend that a machine event was recorded
        // e.g. CASH_COLLECTED, PAPER_REFILLED, DOOR_CLOSED
        Task NotifyNewEvent(
            Guid parkmeterID,
            string eventType);
    }
}