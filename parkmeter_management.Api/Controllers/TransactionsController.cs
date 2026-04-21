using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using parkmeter_management.Api.DTOs;
using parkmeter_management.Api.Mappers;
using parkmeter_management.Application.Managers.Interfaces;

namespace parkmeter_management.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionsController : ControllerBase
    {
        private readonly ITransactionManager _transactionManager;

        public TransactionsController(ITransactionManager transactionManager)
        {
            _transactionManager = transactionManager;
        }

        //----------------------------------------------------------

        [Authorize(Roles = "RESPONSABLE_FINANCIER")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var transactions = await _transactionManager.GetAllTransactions();
            return Ok(transactions.ToDtos());

        }

        //----------------------------------------------------------

        [Authorize(Roles = "RESPONSABLE_FINANCIER")]
        [HttpGet("parkmeter/{parkmeterID}")]
        public async Task<IActionResult> GetByParkmeter(Guid parkmeterID)
        {
            var transactions = await _transactionManager
                .GetByParkmeter(parkmeterID);
            return Ok(transactions.ToDtos());

        }


        //----------------------------------------------------------

        [Authorize(Roles = "RESPONSABLE_FINANCIER")]
        [HttpGet("filter")]
        public async Task<IActionResult> FilterByStatus(
            [FromQuery] string status)
        {
            var transactions = await _transactionManager
                .FilterByStatus(status);
            return Ok(transactions.ToDtos());

        }

        //----------------------------------------------------------

        [Authorize(Roles = "RESPONSABLE_FINANCIER")]
        [HttpGet("session/{sessionId}")]
        public async Task<IActionResult> GetBySession(Guid sessionId)
        {
            var transactions = await _transactionManager
                .GetBySession(sessionId);

            return Ok(transactions.ToDtos());

        }

        //----------------------------------------------------------

        [Authorize(Roles = "RESPONSABLE_FINANCIER")]
        [HttpGet("filter/date")]
        public async Task<IActionResult> FilterByDate(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
        {
            var transactions = await _transactionManager
                .GetByDateRange(from, to);

            return Ok(transactions.ToDtos());

        }

    }
}