using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using parkmeter_management.Api.Mappers;
using parkmeter_management.Application.Managers.Interfaces;

namespace parkmeter_management.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SessionsController : ControllerBase
    {
        private readonly ISessionManager _sessionManager;

        public SessionsController(ISessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        //----------------------------------------------------------

        [Authorize(Roles = "RESPONSABLE_FINANCIER")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var sessions = await _sessionManager.GetAllSessions();
            return Ok(sessions.ToDtos());

        }

        //----------------------------------------------------------

        [Authorize(Roles = "RESPONSABLE_FINANCIER")]
        [HttpGet("parkmeter/{parkmeterID}")]
        public async Task<IActionResult> GetByParkmeter(Guid parkmeterID)
        {
            var sessions = await _sessionManager.GetByParkmeter(parkmeterID);
            return Ok(sessions.ToDtos());

        }

        //----------------------------------------------------------

        [Authorize(Roles = "RESPONSABLE_FINANCIER")]
        [HttpGet("filter/date")]
        public async Task<IActionResult> FilterByDate(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
        {
            var sessions = await _sessionManager
                .GetByDateRange(from, to);

            return Ok(sessions.ToDtos());

        }
    }
}
