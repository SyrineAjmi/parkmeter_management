using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using parkmeter_management.Api.DTOs;
using parkmeter_management.Api.s;
using parkmeter_management.Application.Managers.Implementations;
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


        public ParkmetersController(IParkmeterManager parkmeterManager, 
                                    IAlertManager alertManager,
                                    IParkmeterConfigurationManager configurationManager,
                                    ISessionManager sessionManager)
        {

            _parkmeterManager = parkmeterManager;
            _alertManager = alertManager;
            _configurationManager = configurationManager;
            _sessionManager = sessionManager;

        }



        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var parkmeters = await _parkmeterManager.GetAllParkmeters();
            var dtos = parkmeters.Select(p => new ParkmeterDto
            {
                Id = p.Id,
                SerialNumber = p.SerialNumber,
                SiteId = p.SiteId,
                Status = p.Status.ToString(), 
                CreatedAt = p.CreatedAt
            });
            return Ok(dtos);
        }



        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var parkmeter = await _parkmeterManager.GetById(id);
            if (parkmeter == null) return NotFound();

            var dto = new ParkmeterDto
            {
                Id = parkmeter.Id,
                SerialNumber = parkmeter.SerialNumber,
                SiteId = parkmeter.SiteId,
                Status = parkmeter.Status.ToString(),
                CreatedAt = parkmeter.CreatedAt
            };
            return Ok(dto);
        }



        [HttpGet("site/{siteId}")]
        public async Task<IActionResult> GetBySite(Guid siteId)
        {
            var parkmeters = await _parkmeterManager.GetBySite(siteId);
            var dtos = parkmeters.Select(p => new ParkmeterDto
            {
                Id = p.Id,
                SerialNumber = p.SerialNumber,
                SiteId = p.SiteId,
                Status = p.Status.ToString(),
                CreatedAt = p.CreatedAt
            });
            return Ok(dtos);
        }



        [HttpGet("filter")]
        public async Task<IActionResult> FilterByStatus(
            [FromQuery] string status)
        {
            if (string.IsNullOrEmpty(status))
            {
                var all = await _parkmeterManager.GetAllParkmeters();
                return Ok(all.Select(p => new ParkmeterDto
                {
                    Id = p.Id,
                    SerialNumber = p.SerialNumber,
                    SiteId = p.SiteId,
                    Status = p.Status.ToString(),
                    CreatedAt = p.CreatedAt
                }));
            }

            if (!Enum.TryParse<MeterStatus>(status, true, out var meterStatus))
                return BadRequest("Invalid status value");

            var parkmeters = await _parkmeterManager
                .FilterByStatus(meterStatus); 

            var dtos = parkmeters.Select(p => new ParkmeterDto
            {
                Id = p.Id,
                SerialNumber = p.SerialNumber,
                SiteId = p.SiteId,
                Status = p.Status.ToString(),
                CreatedAt = p.CreatedAt
            });

            return Ok(dtos);
        }



        [Authorize(Roles = "AGENT_INSTALLATION")]
        [HttpPost]
        public async Task<IActionResult> AddParkmeter(
            [FromBody] AddParkmeterRequest request)
        {
            try
            {
                var id = await _parkmeterManager
                    .AddParkmeter(request.SerialNumber, request.SiteId);
                return Ok(new { Id = id, Message = "Parkmeter added successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }



        [Authorize(Roles = "AGENT_INSTALLATION")]
        [HttpPut("{id}/assign-site")]
        public async Task<IActionResult> AssignToSite(
            Guid id, [FromBody] AssignSiteRequest request)
        {
            var result = await _parkmeterManager.AssignToSite(id, request.SiteId);
            if (!result) return NotFound();
            return Ok("Parkmeter assigned to site");
        }



        [Authorize(Roles = "GESTIONNAIRE_SYSTEME")]
        [HttpPut("{id}/status")]
        public async Task<IActionResult> ChangeStatus(
        Guid id, [FromBody] ChangeStatusRequest request)
        {
            Enum.TryParse<MeterStatus>(request.Status, true, out var status);

            var result = await _parkmeterManager.ChangeStatus(id, status);

            if (!result)
            {
                var parkmeter = await _parkmeterManager.GetById(id);
                if (parkmeter == null) return NotFound("Parkmeter not found");
                return BadRequest(
                    "Cannot activate parkmeter without configuration. " +
                    "Create a configuration first.");
            }

            return Ok("Status updated successfully");
        
        }



        [HttpGet("{id}/details")]
        public async Task<IActionResult> GetDetails(Guid id)
        {
            var parkmeter = await _parkmeterManager
                .GetById(id);
            if (parkmeter == null) return NotFound();

            var config = await _configurationManager
                .GetConfiguration(id);

            var activeAlerts = await _alertManager
                .GetByParkmeter(id); 

            var alertDtos = activeAlerts
                .Where(a => a.Status == AlertStatus.NOT_RESOLVED)
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
                }).ToList();

            var sessions = await _sessionManager
                .GetByParkmeter(id);
            var currentSession = sessions
                .FirstOrDefault(s =>
                    s.Status == SessionStatus.ACTIVE); 

            return Ok(new
            {
                Parkmeter = new ParkmeterDto
                {
                    Id = parkmeter.Id,
                    SerialNumber = parkmeter.SerialNumber,
                    SiteId = parkmeter.SiteId,
                    Status = parkmeter.Status.ToString(),
                    CreatedAt = parkmeter.CreatedAt
                },
                Config = config == null ? null : new ParkMeterConfigDto
                {
                    ParkmeterId = config.ParkmeterId,
                    PricePerHour = config.PricePerHour,
                    MaxDuration = config.MaxDuration
                },
                ActiveAlerts = activeAlerts,
                CurrentSession = currentSession == null ? null : new SessionDto
                {
                    Id = currentSession.Id,
                    ParkmeterId = currentSession.ParkmeterId,
                    StartDateTime = currentSession.StartDateTime,
                    TotalTransactions = currentSession.TotalTransactions,
                    TotalAmount = currentSession.TotalAmount,
                    Status = currentSession.Status.ToString()
                }
            });
        }

    }


    public class AddParkmeterRequest
    {
        public string SerialNumber { get; set; } = string.Empty;
        public Guid SiteId { get; set; }
    }


    public class AssignSiteRequest
    {
        public Guid SiteId { get; set; }
    }


    public class ChangeStatusRequest
    {
        public string Status { get; set; } = string.Empty; 
   
    }

}   