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
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var transactions = await _transactionManager
                .GetAllTransactions();

            var total = transactions.Count();
            var paged = transactions
                .OrderByDescending(t => t.CreatedAt)
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

        //----------------------------------------------------------

        [Authorize(Roles = "RESPONSABLE_FINANCIER")]
        [HttpGet("filter/serial")]
        public async Task<IActionResult> FilterBySerial([FromQuery] string serial)
        {
            var transactions = await _transactionManager.GetAllTransactions();
            var filtered = transactions
                .Where(t => t.Parkmeter?.SerialNumber
                    .Contains(serial, StringComparison.OrdinalIgnoreCase) == true);
            return Ok(filtered.ToDtos());
        }

    }
}