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

        //[Authorize(Roles = "RESPONSABLE_FINANCIER")]
        //[HttpGet]
        //public async Task<IActionResult> GetAll(
        //    [FromQuery] int page = 1,
        //    [FromQuery] int pageSize = 20)
        //{
        //    var transactions = await _transactionManager
        //        .GetAllTransactions();

        //    var total = transactions.Count();
        //    var paged = transactions
        //        .OrderByDescending(t => t.CreatedAt)
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
            var transactions = await _transactionManager
                .GetByParkmeter(parkmeterID);
            return Ok(transactions.ToDtos());

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
            var transactions = await _transactionManager.GetAllTransactions();

            // all filters 
            if (!string.IsNullOrEmpty(status))
                transactions = transactions
                    .Where(t => t.Status.ToString() == status).ToList();

            if (!string.IsNullOrEmpty(serial))
                transactions = transactions
                    .Where(t => t.Parkmeter?.SerialNumber
                        .Equals(serial, StringComparison.OrdinalIgnoreCase) == true)
                    .ToList();

            if (from.HasValue)
                transactions = transactions
                    .Where(t => t.CreatedAt >= from.Value).ToList();

            if (to.HasValue)
                transactions = transactions
                    .Where(t => t.CreatedAt <= to.Value).ToList();

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


    }
}