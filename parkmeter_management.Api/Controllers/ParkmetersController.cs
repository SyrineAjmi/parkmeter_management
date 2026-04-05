using Microsoft.AspNetCore.Mvc;
using parkmeter_management.Application.Managers.Interfaces;
using parkmeter_management.Api.DTOs;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ParkmetersController : ControllerBase
    {
        private readonly IParkmeterManager _parkmeterManager;


        public ParkmetersController(IParkmeterManager parkmeterManager)
        {
            _parkmeterManager = parkmeterManager;
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

        

        [HttpPut("{id}/assign-site")]
        public async Task<IActionResult> AssignToSite(
            Guid id, [FromBody] AssignSiteRequest request)
        {
            var result = await _parkmeterManager.AssignToSite(id, request.SiteId);
            if (!result) return NotFound();
            return Ok("Parkmeter assigned to site");
        }



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
            var parkmeters = await _parkmeterManager.GetAllParkmeters();

            if (!string.IsNullOrEmpty(status) &&
                Enum.TryParse<MeterStatus>(status, true, out var meterStatus))
            {
                parkmeters = parkmeters
                    .Where(p => p.Status == meterStatus)
                    .ToList();
            }

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