using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using parkmeter_management.Api.Mappers;
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

        //----------------------------------------------------------

        [Authorize(Roles = "SUPERVISEUR")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var alerts = await _alertManager.GetAllAlerts();
            return Ok(alerts.ToDtos());
        }

        //----------------------------------------------------------

        [Authorize(Roles = "SUPERVISEUR")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var alert = await _alertManager.GetById(id);
            if (alert == null) return NotFound();

            return Ok(alert.ToDto());

        }

        //----------------------------------------------------------

        [Authorize(Roles = "SUPERVISEUR")]
        [HttpPut("{id}/resolve")]
        public async Task<IActionResult> Resolve(Guid id)
        {
            var alert = await _alertManager.GetById(id);
            if (alert == null)
                return NotFound("Alerte introuvable");

            if (alert.Status == AlertStatus.RESOLVED)
                return Conflict("Alerte déjà résolue");

            var result = await _alertManager.ResolveAlert(id);
            if (!result)
                return BadRequest("Impossible de résoudre cette alerte");

            return Ok("Alerte résolue");
        }

        //----------------------------------------------------------

        [Authorize(Roles = "SUPERVISEUR")]
        [HttpGet("filter")]
        public async Task<IActionResult> FilterByPriority(
            [FromQuery] string priority)
        {
            var alerts = await _alertManager.FilterByPriority(priority);
            return Ok(alerts.ToDtos());

        }

        //----------------------------------------------------------

        [Authorize(Roles = "SUPERVISEUR")]
        [HttpGet("unresolved")]
        public async Task<IActionResult> GetUnresolved()
        {
            var alerts = await _alertManager.GetUnresolvedAlerts();
            return Ok(alerts.ToDtos());

        }

        //----------------------------------------------------------

        [Authorize(Roles = "SUPERVISEUR")]
        [HttpGet("parkmeter/{parkmeterId}")]
        public async Task<IActionResult> GetByParkmeter(Guid parkmeterId)
        {
            var alerts = await _alertManager.GetByParkmeter(parkmeterId);
            return Ok(alerts.ToDtos());

        }
    }
}