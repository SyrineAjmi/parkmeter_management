using Microsoft.AspNetCore.Mvc;
using parkmeter_management.Application.Interfaces.Services;
using parkmeter_management.Domain.Enums;
using System.Text.Json;

namespace parkmeter_management.Api.Controllers
{
    /// <summary>
    /// Simulation-only endpoints.
    /// Called by the JS simulator frontend to mimic operator
    /// physical actions on the machine. Each action publishes
    /// to the MQTT events topic exactly as real hardware would.
    /// NOT for production use.
    /// </summary>
    [ApiController]
    [ApiExplorerSettings(GroupName = "simulator")]
    [Route("api/simulator")]
    public class SimulatorController : ControllerBase
    {
        private readonly IMqttPublisherService _mqttPublisher;

        public SimulatorController(IMqttPublisherService mqttPublisher)
        {
            _mqttPublisher = mqttPublisher;
        }

        /// <summary>
        /// Operator physically removes the cash box.
        /// Publishes CASH_COLLECTED event to parkmeters/{id}/events
        /// </summary>
        [HttpPost("cash-removed/{parkmeterID}")]
        public async Task<IActionResult> CashRemoved(Guid parkmeterID)
        {
            await _mqttPublisher.PublishEventAsync(
                parkmeterID,
                ParkMeterEventType.CASH_COLLECTED);

            return Ok(new { Message = "CASH_COLLECTED event published ✅" });
        }

        /// <summary>
        /// Operator refills the paper roll.
        /// Publishes PAPER_REFILLED event to parkmeters/{id}/events
        /// </summary>
        [HttpPost("paper-refilled/{parkmeterID}")]
        public async Task<IActionResult> PaperRefilled(Guid parkmeterID)
        {
            await _mqttPublisher.PublishEventAsync(
                parkmeterID,
                ParkMeterEventType.PAPER_REFILLED);

            return Ok(new { Message = "PAPER_REFILLED event published ✅" });
        }

        /// <summary>
        /// Operator closes the machine door after servicing.
        /// Publishes DOOR_CLOSED event to parkmeters/{id}/events
        /// </summary>
        [HttpPost("door-closed/{parkmeterID}")]
        public async Task<IActionResult> DoorClosed(Guid parkmeterID)
        {
            await _mqttPublisher.PublishEventAsync(
                parkmeterID,
                ParkMeterEventType.DOOR_CLOSED);

            return Ok(new { Message = "DOOR_CLOSED event published ✅" });
        }
    }
}