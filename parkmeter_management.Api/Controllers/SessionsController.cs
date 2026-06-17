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

        //[Authorize(Roles = "RESPONSABLE_FINANCIER")]
        //[HttpGet]
        //public async Task<IActionResult> GetAll(
        //    [FromQuery] int page = 1,
        //    [FromQuery] int pageSize = 20)
        //{
        //    var sessions = await _sessionManager
        //        .GetAllSessions();

        //    var total = sessions.Count();
        //    var paged = sessions
        //        .OrderByDescending(s => s.StartDateTime)
        //        .Skip((page - 1) * pageSize)
        //        .Take(pageSize)
        //        .ToDtos();

        //    return Ok(new
        //    {
        //        Data = paged,
        //        Page = page,
        //        PageSize = pageSize,
        //        Total = total,
        //        TotalPages = (int)Math.Ceiling((double)total / pageSize)
        //    });
        //}

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
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? status = null,
            [FromQuery] string? serial = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
        {
            var sessions = await _sessionManager.GetAllSessions();

            if (!string.IsNullOrEmpty(status))
                sessions = sessions
                    .Where(s => s.Status.ToString() == status).ToList();

            if (!string.IsNullOrEmpty(serial))
                sessions = sessions
                    .Where(s => s.Parkmeter?.SerialNumber
                        .Equals(serial, StringComparison.OrdinalIgnoreCase) == true)
                    .ToList();

            if (from.HasValue)
                sessions = sessions
                    .Where(s => s.StartDateTime >= from.Value).ToList();

            if (to.HasValue)
                sessions = sessions
                    .Where(s => s.StartDateTime <= to.Value).ToList();

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


    }
}
