using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using parkmeter_management.Api.DTOs.Stats;
using parkmeter_management.Application.Queries.Stats;
using parkmeter_management.Application.Queries.Stats.AlertsSummary;
using parkmeter_management.Application.Queries.Stats.CurrentSession;
using parkmeter_management.Application.Queries.Stats.Overview;
using parkmeter_management.Application.Queries.Stats.RevenueParkmeter;
using parkmeter_management.Application.Queries.Stats.RevenueSite;
using parkmeter_management.Application.Queries.Stats.RevenueSummary;

namespace parkmeter_management.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StatsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public StatsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // ── Overview ──────────────────────────────────────────────

        [HttpGet("overview")]
        public async Task<IActionResult> GetOverview()
        {
            var result = await _mediator.Send(
                new GetOverviewQuery());

            return Ok(new OverviewDto
            {
                TotalParkmeters = result.TotalParkmeters,
                InService = result.InService,
                TemporarilyUnavailable = result.TemporarilyUnavailable,
                Maintenance = result.Maintenance,
                OutOfService = result.OutOfService,
                PendingActivation = result.PendingActivation,
                UnresolvedAlerts = result.UnresolvedAlerts,
                PendingInterventionOrders =
                    result.PendingInterventionOrders
            });
        }

        // ── Revenue ───────────────────────────────────────────────

        [Authorize(Roles =
            "RESPONSABLE_FINANCIER,GESTIONNAIRE_SYSTEME")]
        [HttpGet("revenue")]
        public async Task<IActionResult> GetRevenue(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            var dateFrom = from ?? new DateTime(
                DateTime.UtcNow.Year,
                DateTime.UtcNow.Month, 1);
            var dateTo = to ?? DateTime.UtcNow;

            var result = await _mediator.Send(
                new GetRevenueQuery
                {
                    From = dateFrom,
                    To = dateTo
                });

            return Ok(new RevenueSummaryDto
            {
                TotalRevenue = result.TotalRevenue,
                TotalTransactions = result.TotalTransactions,
                CompletedTransactions = result.CompletedTransactions,
                FailedTransactions = result.FailedTransactions,
                CancelledTransactions = result.CancelledTransactions,
                ExpiredTransactions = result.ExpiredTransactions,
                From = result.From,
                To = result.To
            });
        }

        [Authorize(Roles =
            "RESPONSABLE_FINANCIER,GESTIONNAIRE_SYSTEME")]
        [HttpGet("revenue/parkmeters")]
        public async Task<IActionResult> GetRevenueByParkmeter(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            var dateFrom = from ?? new DateTime(
                DateTime.UtcNow.Year,
                DateTime.UtcNow.Month, 1);
            var dateTo = to ?? DateTime.UtcNow;

            var results = await _mediator.Send(
                new GetRevenueByParkmeterQuery
                {
                    From = dateFrom,
                    To = dateTo
                });

            return Ok(results.Select(r =>
                new RevenueByParkmeterDto
                {
                    ParkmeterID = r.ParkmeterID,
                    SerialNumber = r.SerialNumber,
                    TotalRevenue = r.TotalRevenue,
                    TotalTransactions = r.TotalTransactions
                }));
        }

        [Authorize(Roles =
            "RESPONSABLE_FINANCIER,GESTIONNAIRE_SYSTEME")]
        [HttpGet("revenue/sites")]
        public async Task<IActionResult> GetRevenueBySite(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            var dateFrom = from ?? new DateTime(
                DateTime.UtcNow.Year,
                DateTime.UtcNow.Month, 1);
            var dateTo = to ?? DateTime.UtcNow;

            var results = await _mediator.Send(
                new GetRevenueBySiteQuery
                {
                    From = dateFrom,
                    To = dateTo
                });

            return Ok(results.Select(r =>
                new RevenueBySiteDto
                {
                    SiteId = r.SiteId,
                    SiteName = r.SiteName,
                    TotalRevenue = r.TotalRevenue,
                    TotalTransactions = r.TotalTransactions
                }));
        }

        // ── Alerts ────────────────────────────────────────────────

        [HttpGet("alerts")]
        public async Task<IActionResult> GetAlertsSummary()
        {
            var result = await _mediator.Send(
                new GetAlertsSummaryQuery());

            return Ok(new AlertsSummaryDto
            {
                TotalUnresolved = result.TotalUnresolved,
                Low = result.Low,
                High = result.High,
                Normal = result.Normal,
                ByType = result.ByType
            });
        }

        // ── Sessions ──────────────────────────────────────────────

        [HttpGet("sessions/{parkmeterID}/current")]
        public async Task<IActionResult> GetCurrentSession(
            Guid parkmeterID)
        {
            var result = await _mediator.Send(
                new GetCurrentSessionQuery
                {
                    ParkmeterID = parkmeterID
                });

            if (result == null)
                return NotFound(
                    "No active session for this parkmeter");

            return Ok(new CurrentSessionDto
            {
                ParkmeterID = result.ParkmeterID,
                SerialNumber = result.SerialNumber,
                SessionStart = result.SessionStart,
                TotalTransactions = result.TotalTransactions,
                TotalAmount = result.TotalAmount,
                Status = result.Status
            });
        }
    }
}