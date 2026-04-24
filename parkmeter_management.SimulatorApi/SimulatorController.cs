using MediatR;
using Microsoft.AspNetCore.Mvc;
using parkmeter_management.SimulatorApplication
    .Commands.HandleCashRemoved;
using parkmeter_management.SimulatorApplication
    .Commands.HandleDoorClosed;
using parkmeter_management.SimulatorApplication
    .Commands.HandlePaperRefilled;

namespace parkmeter_management.SimulatorApi.Controllers
{
    /// <summary>
    /// Simulation-only endpoints.
    /// Represents physical sensor actions that in
    /// production would be triggered by real hardware.
    /// NOT for production use.
    /// </summary>
    [ApiController]
    [Route("api/simulator")]
    public class SimulatorController : ControllerBase
    {
        private readonly IMediator _mediator;

        public SimulatorController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("cash-removed/{parkmeterID}")]
        public async Task<IActionResult> CashRemoved(
            Guid parkmeterID)
        {
            await _mediator.Send(
                new HandleCashRemovedCommand
                { ParkmeterId = parkmeterID });
            return Ok(new { Message = "Cash removed ✅" });
        }

        [HttpPost("paper-refilled/{parkmeterID}")]
        public async Task<IActionResult> PaperRefilled(
            Guid parkmeterID)
        {
            await _mediator.Send(
                new HandlePaperRefilledCommand
                { ParkmeterId = parkmeterID });
            return Ok(new { Message = "Paper refilled ✅" });
        }

        [HttpPost("door-closed/{parkmeterID}")]
        public async Task<IActionResult> DoorClosed(
            Guid parkmeterID)
        {
            await _mediator.Send(
                new HandleDoorClosedCommand
                { ParkmeterId = parkmeterID });
            return Ok(new { Message = "Door closed ✅" });
        }
    }
}