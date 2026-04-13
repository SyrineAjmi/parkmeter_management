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
    }
}