using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using parkmeter_management.Application.Queries.AI;

namespace parkmeter_management.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AiController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AiController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("financial-report")]
        [Authorize(Roles = "RESPONSABLE_FINANCIER")]
        public async Task<IActionResult> GenerateFinancialReport(
            [FromBody] GenerateFinancialReportRequest request)
        {
            if (request.Month < 1 || request.Month > 12)
                return BadRequest("Month must be between 1 and 12.");

            if (request.Year < 2000 || request.Year > DateTime.UtcNow.Year)
                return BadRequest("Invalid year.");

            var report = await _mediator.Send(new GenerateFinancialReportQuery
            {
                Month = request.Month,
                Year = request.Year
            });

            return Ok(report);
        }
    }

    public class GenerateFinancialReportRequest
    {
        public int Month { get; set; } = DateTime.UtcNow.Month;
        public int Year { get; set; } = DateTime.UtcNow.Year;
    }
}