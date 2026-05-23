using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using parkmeter_management.Api.Mappers;
using parkmeter_management.Api.Requests.Parkmeter;
using parkmeter_management.Application.Managers.Interfaces;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ParkmetersController : ControllerBase
    {
        private readonly IParkmeterManager _parkmeterManager;
        private readonly IAlertManager _alertManager;
        private readonly IParkmeterConfigurationManager _configurationManager;
        private readonly ISessionManager _sessionManager;

        public ParkmetersController(
            IParkmeterManager parkmeterManager,
            IAlertManager alertManager,
            IParkmeterConfigurationManager configurationManager,
            ISessionManager sessionManager)
        {
            _parkmeterManager = parkmeterManager;
            _alertManager = alertManager;
            _configurationManager = configurationManager;
            _sessionManager = sessionManager;
        }

        //----------------------------------------------------------

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var parkmeters = await _parkmeterManager
                .GetAllParkmeters();
            return Ok(parkmeters.ToDtos());
        }

        //----------------------------------------------------------

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var parkmeter = await _parkmeterManager.GetById(id);
            if (parkmeter == null) return NotFound();
            return Ok(parkmeter.ToDto());
        }

        //----------------------------------------------------------

        [HttpGet("site/{siteId}")]
        public async Task<IActionResult> GetBySite(Guid siteId)
        {
            var parkmeters = await _parkmeterManager
                .GetBySite(siteId);
            return Ok(parkmeters.ToDtos());
        }

        //----------------------------------------------------------

        [HttpGet("filter")]
        public async Task<IActionResult> FilterByStatus(
            [FromQuery] string status)
        {
            if (string.IsNullOrEmpty(status))
            {
                var all = await _parkmeterManager.GetAllParkmeters();
                return Ok(all.ToDtos());
            }

            if (!Enum.TryParse<MeterStatus>(
                status, true, out var meterStatus))
                return BadRequest("Invalid status value");

            var parkmeters = await _parkmeterManager
                .FilterByStatus(meterStatus);
            return Ok(parkmeters.ToDtos());
        }

        //----------------------------------------------------------

        [Authorize(Roles = "AGENT_INSTALLATION")]
        [HttpPost]
        public async Task<IActionResult> AddParkmeter(
            [FromBody] AddParkmeterRequest request)
        {
            try
            {
                var id = await _parkmeterManager
                    .AddParkmeter(
                        request.SerialNumber,
                        request.SiteId);
                return Ok(new
                {
                    Id = id,
                    Message = "Parkmeter added successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        //----------------------------------------------------------

        [Authorize(Roles = "AGENT_INSTALLATION, GESTIONNAIRE_SYSTEME")]
        [HttpPut("{id}/assign-site")]
        public async Task<IActionResult> AssignToSite(
            Guid id, [FromBody] AssignSiteRequest request)
        {
            var result = await _parkmeterManager
                .AssignToSite(id, request.SiteId);
            if (!result) return NotFound();
            return Ok("Parkmeter assigned to site");
        }

        //----------------------------------------------------------

        [Authorize(Roles = "GESTIONNAIRE_SYSTEME")]
        [HttpPut("{id}/status")]
        public async Task<IActionResult> ChangeStatus(
            Guid id, [FromBody] ChangeStatusRequest request)
        {
            if (!Enum.TryParse<MeterStatus>(
                request.Status, true, out var status))
                return BadRequest("Invalid status value");

            var result = await _parkmeterManager
                .ChangeStatus(id, status);

            if (!result)
            {
                var parkmeter = await _parkmeterManager.GetById(id);
                if (parkmeter == null)
                    return NotFound("Parkmeter not found");
                return BadRequest(
                    "Commande non envoyée — " +
                    "parcmètre injoignable ou connexion MQTT perdue.");
            }

            return Ok("Status updated successfully");
        }

        //----------------------------------------------------------

        [HttpGet("{id}/details")]
        public async Task<IActionResult> GetDetails(Guid id)
        {
            var parkmeter = await _parkmeterManager.GetById(id);
            if (parkmeter == null) return NotFound();

            var config = await _configurationManager
                .GetConfiguration(id);

     
            var alertDtos = (await _alertManager
                .GetUnresolvedByParkmeter(id))
                .ToDtos()
                .ToList();

            var sessions = await _sessionManager.GetByParkmeter(id);
            var currentSession = sessions
                .FirstOrDefault(s =>
                    s.Status == SessionStatus.ACTIVE);

            return Ok(new
            {
                Parkmeter = parkmeter.ToDto(),
                Config = config?.ToDto(),
                ActiveAlerts = alertDtos,
                CurrentSession = currentSession?.ToDto()
            });
        }
    }
}