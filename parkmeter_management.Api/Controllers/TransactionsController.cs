using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using parkmeter_management.Api.DTOs;
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


        [Authorize(Roles = "RESPONSABLE_FINANCIER")]
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




        [Authorize(Roles = "RESPONSABLE_FINANCIER")]
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




        [Authorize(Roles = "RESPONSABLE_FINANCIER")]
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


        [Authorize(Roles = "RESPONSABLE_FINANCIER")]
        [HttpGet("session/{sessionId}")]
        public async Task<IActionResult> GetBySession(Guid sessionId)
        {
            var transactions = await _transactionManager
                .GetBySession(sessionId);

            var dtos = transactions
                .Select(t => new TransactionDto
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


        [Authorize(Roles = "RESPONSABLE_FINANCIER")]
        [HttpGet("filter/date")]
        public async Task<IActionResult> FilterByDate(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
        {
            var transactions = await _transactionManager
                .GetByDateRange(from, to);

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