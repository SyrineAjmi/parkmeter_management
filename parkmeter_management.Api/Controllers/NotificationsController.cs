using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Domain.Enums;
using System.Security.Claims;

namespace parkmeter_management.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly IAlertRepository _alertRepository;
        private readonly ISessionRepository _sessionRepository;
        private readonly IInterventionOrderRepository _orderRepository;

        public NotificationsController(
            IAlertRepository alertRepository,
            ISessionRepository sessionRepository,
            IInterventionOrderRepository orderRepository)
        {
            _alertRepository = alertRepository;
            _sessionRepository = sessionRepository;
            _orderRepository = orderRepository;
        }


        private static string GetAlertTypeLabel(AlertType type) => type switch
        {
            AlertType.CASH_BOX_FULL => "Caisse pleine",
            AlertType.CASH_BOX_WARN => "Caisse presque pleine",
            AlertType.PAPER_LOW => "Papier faible",
            AlertType.PAPER_EMPTY => "Plus de papier (vide)",
            AlertType.DOOR_OPEN => "Porte ouverte",
            AlertType.HARDWARE_FAILURE => "Panne matérielle",
            AlertType.CONNECTION_LOST => "Connexion perdue",
            _ => type.ToString()
        };

        private static string GetPriorityLabel(Priority priority) => priority switch
        {
            Priority.HIGH => "Élevée",
            Priority.NORMAL => "Moyenne",
            Priority.LOW => "Faible",
            _ => priority.ToString()
        };

        private static string GetInterventionTypeLabel(InterventionType type) => type switch
        {
            InterventionType.CASH_COLLECTION => "Collecte caisse",
            InterventionType.PAPER_REFILL => "Recharge papier",
            InterventionType.HARDWARE_REPAIR => "Réparation matérielle",
            _ => type.ToString()
        };



        [HttpGet("history")]
        public async Task<IActionResult> GetHistory()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var since = DateTime.UtcNow.AddHours(-24);
            var items = new List<object>();

            if (role == "SUPERVISEUR" || role == "GESTIONNAIRE_SYSTEME")
            {
                var alerts = await _alertRepository.GetAllAsync();
                foreach (var a in alerts.Where(a => a.CreatedAt >= since))
                {
                    items.Add(new
                    {
                        id = a.Id,
                        type = a.Status == AlertStatus.RESOLVED ? "resolved" : "alert",
                        title = a.Status == AlertStatus.RESOLVED ? "Alerte résolue" : "Nouvelle alerte",
                        message = $"[{a.Parkmeter?.SerialNumber}] {GetAlertTypeLabel(a.Type)} — {GetPriorityLabel(a.Priority)}",
                        timestamp = a.Status == AlertStatus.RESOLVED
                            ? a.ResolvedAt ?? a.CreatedAt
                            : a.CreatedAt,
                        color = a.Status == AlertStatus.RESOLVED ? "#22c55e" : "#ef4444",
                        read = true
                    });
                }
            }

            if (role == "RESPONSABLE_FINANCIER")
            {
                var sessions = await _sessionRepository.GetAllAsync();
                foreach (var s in sessions.Where(s =>
                    s.Status == SessionStatus.CLOSED &&
                    s.EndDateTime.HasValue &&
                    s.EndDateTime.Value >= since))
                {
                    items.Add(new
                    {
                        id = s.Id,
                        type = "session",
                        title = "Session clôturée",
                        message = $"[{s.Parkmeter?.SerialNumber}] {s.TotalAmount:F3} DT — {s.TotalTransactions} tx",
                        timestamp = s.EndDateTime,
                        color = "#7c3aed",
                        read = true
                    });
                }
            }

            if (role == "SUPERVISEUR" || role == "OPERATEUR")
            {
                var orders = await _orderRepository.GetAllAsync();

                if (role == "OPERATEUR" && Guid.TryParse(userId, out var operatorGuid))
                    orders = orders.Where(o => o.OperatorId == operatorGuid);

                foreach (var o in orders.Where(o => o.CreatedAt >= since))
                {
                    items.Add(new
                    {
                        id = o.Id,
                        type = o.Status == InterventionOrderStatus.COMPLETED
                            ? "order_done" : "order",
                        title = o.Status == InterventionOrderStatus.COMPLETED
                            ? "Intervention terminée" : "Nouvel ordre d'intervention",
                        message = $"[{o.Parkmeter?.SerialNumber}] {GetInterventionTypeLabel(o.Type)}",
                        timestamp = o.Status == InterventionOrderStatus.COMPLETED
                            ? o.CompletedAt ?? o.CreatedAt
                            : o.CreatedAt,
                        color = o.Status == InterventionOrderStatus.COMPLETED
                            ? "#22c55e" : "#f59e0b",
                        read = false
                    });
                }
            }

            var result = items
                .OrderByDescending(i => (DateTime)((dynamic)i).timestamp)
                .Take(50)
                .ToList();

            return Ok(result);
        }
    }
}