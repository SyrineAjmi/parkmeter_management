namespace parkmeter_management.Application.Interfaces.Services
{
    public interface INotificationService
    {
        Task NotifyNewAlert(
            Guid parkmeterID,
            string serialNumber,
            string alertType,
            string priority);

        Task NotifyAlertResolved(
            Guid alertId,
            string serialNumber,
            Guid parkmeterID);
        Task NotifyParkmeterOnline(
            Guid parkmeterID,
            string serialNumber);

        Task NotifyStatusChanged(
            Guid parkmeterID,
            string serialNumber,
            string newStatus);

        Task NotifyNewTransaction(
            Guid parkmeterID,
            decimal amount,
            string status);

        Task NotifySessionClosed(
            Guid parkmeterID,
            string serialNumber,
            decimal totalAmount,
            int totalTransactions);

        Task NotifyInterventionOrderCreated(
            Guid operatorId,
            string serialNumber,
            Guid parkmeterID,
            Guid orderId,
            string interventionType);

        Task NotifyInterventionOrderCompleted(
            Guid orderId,
            string serialNumber,
            Guid parkmeterID,
            Guid operatorId,
            DateTime completedAt);

        Task NotifyTelemetry(
            Guid parkmeterID,
            decimal cashAmount,
            int transactionCount);
    }
   
    }