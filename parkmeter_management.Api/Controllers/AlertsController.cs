using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using parkmeter_management.Api.DTOs;
using parkmeter_management.Application.Commands.Alerts;
using parkmeter_management.Application.Managers.Interfaces;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Api.Controllers
{
    
    [ApiController]
    [Route("api/[controller]")]
    public class AlertsController : ControllerBase
    {
        private readonly IAlertManager _alertManager;

        public AlertsController(IAlertManager alertManager)
        {
            _alertManager = alertManager;
        }


        [Authorize(Roles = "SUPERVISEUR")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var alerts = await _alertManager.GetAllAlerts();
            var dtos = alerts.Select(a => new AlertDto
            {
                Id = a.Id,
                ParkmeterId = a.ParkmeterId,
                Type = a.Type.ToString(),
                Priority = a.Priority.ToString(),
                Status = a.Status.ToString(),
                Description = a.Description,
                CreatedAt = a.CreatedAt,
                ResolvedAt = a.ResolvedAt
            });
            return Ok(dtos);
        }


        [Authorize(Roles = "SUPERVISEUR")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var alert = await _alertManager.GetById(id);
            if (alert == null) return NotFound();

            var dto = new AlertDto
            {
                Id = alert.Id,
                ParkmeterId = alert.ParkmeterId,
                Type = alert.Type.ToString(),
                Priority = alert.Priority.ToString(),
                Status = alert.Status.ToString(),
                Description = alert.Description,
                CreatedAt = alert.CreatedAt,
                ResolvedAt = alert.ResolvedAt
            };
            return Ok(dto);
        }

        [Authorize(Roles = "SUPERVISEUR")]
        [HttpPut("{id}/resolve")]
        public async Task<IActionResult> Resolve(Guid id)
        {
            var result = await _alertManager.ResolveAlert(id);
            if (!result) return NotFound();
            return Ok("Alert resolved successfully");
        }

        [Authorize(Roles = "SUPERVISEUR")]
        [HttpGet("filter")]
        public async Task<IActionResult> FilterByPriority(
            [FromQuery] string priority)
        {
            var alerts = await _alertManager.FilterByPriority(priority);
            var dtos = alerts.Select(a => new AlertDto
            {
                Id = a.Id,
                ParkmeterId = a.ParkmeterId,
                Type = a.Type.ToString(),
                Priority = a.Priority.ToString(),
                Status = a.Status.ToString(),
                Description = a.Description,
                CreatedAt = a.CreatedAt,
                ResolvedAt = a.ResolvedAt
            });
            return Ok(dtos);
        }


        [Authorize(Roles = "SUPERVISEUR")]
        [HttpGet("unresolved")]
        public async Task<IActionResult> GetUnresolved()
        {
            var alerts = await _alertManager.GetUnresolvedAlerts(); 
            var dtos = alerts
                .Select(a => new AlertDto
                {
                    Id = a.Id,
                    ParkmeterId = a.ParkmeterId,
                    Type = a.Type.ToString(),
                    Priority = a.Priority.ToString(),
                    Status = a.Status.ToString(),
                    Description = a.Description,
                    CreatedAt = a.CreatedAt,
                    ResolvedAt = a.ResolvedAt
                });
            return Ok(dtos);
        }

        [Authorize(Roles = "SUPERVISEUR")]
        [HttpGet("parkmeter/{parkmeterId}")]
        public async Task<IActionResult> GetByParkmeter(Guid parkmeterId)
        {
            var alerts = await _alertManager.GetByParkmeter(parkmeterId); 
            var dtos = alerts
                .Select(a => new AlertDto
                {
                    Id = a.Id,
                    ParkmeterId = a.ParkmeterId,
                    Type = a.Type.ToString(),
                    Priority = a.Priority.ToString(),
                    Status = a.Status.ToString(),
                    Description = a.Description,
                    CreatedAt = a.CreatedAt,
                    ResolvedAt = a.ResolvedAt
                });
            return Ok(dtos);
        }
    }
}