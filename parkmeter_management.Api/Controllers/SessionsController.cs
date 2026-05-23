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
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var sessions = await _sessionManager
                .GetAllSessions();

            var total = sessions.Count();
            var paged = sessions
                .OrderByDescending(s => s.StartDateTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToDtos();

            return Ok(new
            {
                Data = paged,
                Page = page,
                PageSize = pageSize,
                Total = total,
                TotalPages = (int)Math.Ceiling((double)total / pageSize)
            });
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

        //----------------------------------------------------------

        [Authorize(Roles = "RESPONSABLE_FINANCIER")]
        [HttpGet("filter/status")]
        public async Task<IActionResult> FilterByStatus([FromQuery] string status)
        {
            var sessions = await _sessionManager.GetAllSessions();
            var filtered = sessions
                .Where(s => s.Status.ToString() == status);
            return Ok(filtered.ToDtos());
        }

        //----------------------------------------------------------

        [Authorize(Roles = "RESPONSABLE_FINANCIER")]
        [HttpGet("filter/serial")]
        public async Task<IActionResult> FilterBySerial([FromQuery] string serial)
        {
            var sessions = await _sessionManager.GetAllSessions();
            var filtered = sessions
                .Where(s => s.Parkmeter?.SerialNumber
                    .Contains(serial, StringComparison.OrdinalIgnoreCase) == true);
            return Ok(filtered.ToDtos());
        }
    }
}
