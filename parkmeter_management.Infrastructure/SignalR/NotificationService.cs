using Microsoft.AspNetCore.SignalR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Application.Interfaces.Services;
using parkmeter_management.Domain.Entities;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Infrastructure.SignalR
{
    public class NotificationService : INotificationService
    {
        private readonly IHubContext<ParkMeterHub> _hubContext;
        private readonly INotificationRepository _notifRepo;
        private readonly IUserRepository _userRepo;
        private static readonly Dictionary<string, string> InterventionTypeLabels = new()
        {
            ["CASH_COLLECTION"] = "Collecte de caisse",
            ["PAPER_REFILL"] = "Rechargement papier",
            ["HARDWARE_REPAIR"] = "Réparation matérielle"
        };

        private static string TranslateType(string type)
            => InterventionTypeLabels.GetValueOrDefault(type, type);

        private static readonly Dictionary<string, string> AlertTypeLabels = new()
        {
            ["CASH_BOX_WARN"] = "Avertissement caisse",
            ["CASH_BOX_FULL"] = "Caisse pleine",
            ["PAPER_LOW"] = "Papier faible",
            ["PAPER_EMPTY"] = "Papier vide",
            ["HARDWARE_FAILURE"] = "Panne matérielle",
            ["DOOR_OPEN"] = "Porte ouverte",
            ["CONNECTION_LOST"] = "Connexion perdue"
        };

        private static string TranslateAlert(string alertType)
            => AlertTypeLabels.GetValueOrDefault(alertType, alertType);

        public NotificationService(
            IHubContext<ParkMeterHub> hubContext,
             INotificationRepository notifRepo,
             IUserRepository userRepo)

        {
            _hubContext = hubContext;
            _notifRepo = notifRepo;
            _userRepo = userRepo;
        }

        private async Task SaveToUsersAsync(
            string[] roles,
            string title,
            string message,
            string type,
            Guid? referenceId = null)
        {
            var users = await _userRepo.GetAllAsync();
            var targetUsers = users.Where(u => roles.Contains(u.Role.ToString())).ToList();

            foreach (var user in targetUsers)
            {
                await _notifRepo.AddAsync(new Notification
                (
                    user.Id,
                    title,
                    message,
                    type,
                    referenceId
                ));
            }
        }

        public async Task NotifyNewAlert(
            Guid parkmeterID,
            string serialNumber,
            string alertType,
            string priority)
        {
            await SaveToUsersAsync(
                new[] { "SUPERVISEUR", "GESTIONNAIRE_SYSTEME" },
                $"Nouvelle alerte — {serialNumber}",
                $"Alerte {TranslateAlert(alertType)} ({priority}) détectée sur {serialNumber}",
                "ALERT",
                parkmeterID);

            await _hubContext.Clients
                .Groups("SUPERVISEUR", "GESTIONNAIRE_SYSTEME")
                .SendAsync(
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

            await SaveToUsersAsync(
                new[] { "SUPERVISEUR", "GESTIONNAIRE_SYSTEME" },
                $"Résolution d'alerte — {serialNumber}",
                $"Alerte résolue sur {serialNumber}",
                "ALERT",
                parkmeterID);

            await _hubContext.Clients
                .Groups("SUPERVISEUR", "GESTIONNAIRE_SYSTEME")
                .SendAsync(
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
            //await SaveToUsersAsync(
            //    new[] { "SUPERVISEUR", "GESTIONNAIRE_SYSTEME", "AGENT_INSTALLATION" },
            //    $"Parcmètre en ligne — {serialNumber}",
            //    $"Parcmètre en ligne détecté  {serialNumber}",
            //    "En ligne",
            //    parkmeterID);

            await _hubContext.Clients
                .Groups("SUPERVISEUR", "GESTIONNAIRE_SYSTEME", "AGENT_INSTALLATION")
                .SendAsync(
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
            //await SaveToUsersAsync(
            //    new[] { "SUPERVISEUR", "GESTIONNAIRE_SYSTEME", "AGENT_INSTALLATION" },
            //    $"Changement statut — {serialNumber}",
            //    $"Statut changé de {serialNumber} à {newStatus}",
            //    "STATUS",
            //    parkmeterID);

            await _hubContext.Clients
                .Groups("SUPERVISEUR", "GESTIONNAIRE_SYSTEME", "AGENT_INSTALLATION")
                .SendAsync(
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

            await SaveToUsersAsync(
                new[] { "RESPONSABLE_FINANCIER", "GESTIONNAIRE_SYSTEME" },
                $"Session cloturée — {serialNumber}",
                $"Session avec {totalAmount} DT et {totalTransactions} transactions",
                "SESSION",
                parkmeterID);

            await _hubContext.Clients
                .Groups("RESPONSABLE_FINANCIER", "GESTIONNAIRE_SYSTEME")
                .SendAsync(
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
            Guid orderId,
            string interventionType)
        {

            await SaveToUsersAsync(
                new[] { "SUPERVISEUR" },
                $"Nouvel ordre — {serialNumber}",
                $"Ordre {TranslateType(interventionType)} créé sur {serialNumber}",
                "ORDRE",
                parkmeterID);

            await _notifRepo.AddAsync(new Notification(
                operatorId,
                $"Nouvel ordre assigné — {serialNumber}",
                $"Ordre {TranslateType(interventionType)} sur {serialNumber} vous a été assigné",
                "ORDRE",
                orderId
            ));

            await _hubContext.Clients
                .Group("SUPERVISEUR")
                .SendAsync(
                "InterventionOrderCreated", new
                {
                    OperatorId = operatorId,
                    SerialNumber = serialNumber,
                    ParkmeterID = parkmeterID,
                    OrderId = orderId,
                    InterventionType = interventionType, 
                    Timestamp = DateTime.UtcNow
                });

            //specific operator to get the order assigned to him 
            await _hubContext.Clients
                .Group(operatorId.ToString())
                .SendAsync(
                "InterventionOrderCreated", new 
                { 
                    OperatorId = operatorId, 
                    SerialNumber = serialNumber, 
                    ParkmeterID = parkmeterID, 
                    OrderId = orderId, 
                    InterventionType = interventionType,
                    Timestamp = DateTime.UtcNow 
                });
        }

        public async Task NotifyInterventionOrderCompleted(
            Guid orderId,
            string serialNumber,
            Guid parkmeterID,
            Guid operatorId,    
            DateTime completedAt)
        {

            await SaveToUsersAsync(
                new[] { "SUPERVISEUR" },
                $"Ordre complété — {serialNumber}",
                $"Ordre d'intervention sur {serialNumber} complété",
                "ORDRE",
                parkmeterID);

            await _notifRepo.AddAsync(new Notification(
               operatorId,
               $"Ordre complété — {serialNumber}",
               $"Intervention sur {serialNumber} enregistrée",
               "ORDRE",
               orderId
           ));

            await _hubContext.Clients
                .Group("SUPERVISEUR")
                .SendAsync(
                "InterventionOrderCompleted", new
                {
                    OrderId = orderId,
                    SerialNumber = serialNumber,
                    ParkmeterID = parkmeterID,
                    CompletedAt = completedAt
                });

            //specific op 
            await _hubContext.Clients
                .Group(operatorId.ToString())
                .SendAsync(
                "InterventionOrderCompleted", new { 
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