using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using parkmeter_management.Api.Mappers;
using parkmeter_management.Application.Managers.Interfaces;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InterventionOrdersController : ControllerBase
    {
        private readonly IInterventionOrderManager
            _interventionOrderManager;

        public InterventionOrdersController(
            IInterventionOrderManager interventionOrderManager)
        {
            _interventionOrderManager = interventionOrderManager;
        }

        [Authorize(Roles = "SUPERVISEUR")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var orders = await _interventionOrderManager
                .GetAllOrders();
            return Ok(orders.ToDtos());
        }

        [Authorize(Roles = "SUPERVISEUR")]
        [HttpGet("pending")]
        public async Task<IActionResult> GetPending()
        {
            var orders = await _interventionOrderManager
                .GetPendingOrders();
            return Ok(orders.ToDtos());
        }

        [Authorize(Roles = "SUPERVISEUR")]
        [HttpGet("parkmeter/{parkmeterID}")]
        public async Task<IActionResult> GetByParkmeter(
            Guid parkmeterID)
        {
            var orders = await _interventionOrderManager
                .GetByParkmeter(parkmeterID);
            return Ok(orders.ToDtos());
        }

        [Authorize(Roles = "SUPERVISEUR")]
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateInterventionOrderRequest request)
        {
            var id = await _interventionOrderManager
                .CreateOrder(request.ParkmeterId, request.OperatorId,request.Type);
            return Ok(new
            {
                Id = id,
                Message = "interv order created"+"and operator notified"
            });
        }

        [Authorize(Roles = "OPERATEUR")]
        [HttpGet("operator/{operatorId}")]
        public async Task<IActionResult> GetByOperator(
            Guid operatorId)
        {
            var orders = await _interventionOrderManager
                .GetByOperator(operatorId);
            return Ok(orders.ToDtos());
        }
    }


    public class CreateInterventionOrderRequest
    {
        public Guid ParkmeterId { get; set; }
        public Guid OperatorId { get; set; }
        public InterventionType Type { get; set; }
        = InterventionType.CASH_COLLECTION;
    }

}