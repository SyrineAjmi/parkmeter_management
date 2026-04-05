using Microsoft.AspNetCore.Mvc;
using parkmeter_management.Application.Managers.Interfaces;
using parkmeter_management.Api.DTOs;

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

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var transactions = await _transactionManager.GetAllTransactions();
            var dtos = transactions.Select(t => new TransactionDto
            {
                Id = t.Id,
                ParkmeterId = t.ParkmeterId,
                SessionId = t.SessionId,
                Amount = t.Amount,
                Status = t.Status.ToString(),
                CreatedAt = t.CreatedAt
            });
            return Ok(dtos);
        }

        [HttpGet("parkmeter/{parkmeterID}")]
        public async Task<IActionResult> GetByParkmeter(Guid parkmeterID)
        {
            var transactions = await _transactionManager
                .GetByParkmeter(parkmeterID);
            var dtos = transactions.Select(t => new TransactionDto
            {
                Id = t.Id,
                ParkmeterId = t.ParkmeterId,
                SessionId = t.SessionId,
                Amount = t.Amount,
                Status = t.Status.ToString(),
                CreatedAt = t.CreatedAt
            });
            return Ok(dtos);
        }

        [HttpGet("filter")]
        public async Task<IActionResult> FilterByStatus(
            [FromQuery] string status)
        {
            var transactions = await _transactionManager
                .FilterByStatus(status);
            var dtos = transactions.Select(t => new TransactionDto
            {
                Id = t.Id,
                ParkmeterId = t.ParkmeterId,
                SessionId = t.SessionId,
                Amount = t.Amount,
                Duration = t.Duration,
                Status = t.Status.ToString(),
                CreatedAt = t.CreatedAt
            });
            return Ok(dtos);
        }
    }
}