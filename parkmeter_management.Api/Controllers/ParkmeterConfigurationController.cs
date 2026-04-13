using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using parkmeter_management.Application.Managers.Interfaces;

namespace parkmeter_management.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ParkmeterConfigurationController : ControllerBase
    {
        private readonly IParkmeterConfigurationManager _configurationManager;

        public ParkmeterConfigurationController(
            IParkmeterConfigurationManager configurationManager)
        {
            _configurationManager = configurationManager;
        }



        //all
        [HttpGet("{parkmeterId}")]
        public async Task<IActionResult> GetConfiguration(Guid parkmeterId)
        {
            var config = await _configurationManager
                .GetConfiguration(parkmeterId);
            if (config == null) return NotFound();
            return Ok(config);
        }




        [Authorize(Roles = "AGENT_INSTALLATION")]
        [HttpPost("create")]
        public async Task<IActionResult> CreateConfig(
            [FromBody] CreateConfigRequest request)
        {
            var result = await _configurationManager.CreateConfig(
                request.ParkmeterId,
                request.PricePerHour,
                request.MaxDuration);
            return Ok("Config created and sent to parkmeter");
        }




        [Authorize(Roles = "AGENT_INSTALLATION")]
        [HttpPut("update")]
        public async Task<IActionResult> UpdateConfig(
            [FromBody] UpdateConfigRequest request)
        {
            var result = await _configurationManager.UpdateConfig(
                request.ParkmeterId,
                request.PricePerHour,
                request.MaxDuration);
            if (!result) return NotFound("Config not found for this parkmeter");
            return Ok("Config updated and sent to parkmeter");
        }




        [Authorize(Roles = "GESTIONNAIRE_SYSTEME")]
        [HttpDelete("{parkmeterId}")]
        public async Task<IActionResult> DeleteConfig(Guid parkmeterId)
        {
            var result = await _configurationManager.DeleteConfig(parkmeterId);
            if (!result) return NotFound();
            return Ok("Config deleted");
        }




        [Authorize(Roles = "AGENT_INSTALLATION")]
        [HttpPost("{parkmeterId}/apply")]
        public async Task<IActionResult> ApplyToParkmeter(Guid parkmeterId)
        {
            var result = await _configurationManager
                .ApplyToParkmeter(parkmeterId);
            if (!result) return NotFound("No config found for this parkmeter");
            return Ok("Config re-applied to parkmeter");
        }
    }




    // Request models
    public class CreateConfigRequest
    {
        public Guid ParkmeterId { get; set; }
        public decimal PricePerHour { get; set; }
        public int MaxDuration { get; set; }
    }

    public class UpdateConfigRequest
    {
        public Guid ParkmeterId { get; set; }
        public decimal PricePerHour { get; set; }
        public int MaxDuration { get; set; }
    }
}