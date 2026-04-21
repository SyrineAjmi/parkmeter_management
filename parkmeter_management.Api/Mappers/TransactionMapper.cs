using parkmeter_management.Api.DTOs;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Api.Mappers
{
    public static class TransactionMapper
    {
        public static TransactionDto ToDto(this Transaction transaction)
            => new TransactionDto
            {
                Id = transaction.Id,
                ParkmeterId = transaction.ParkmeterId,
                SessionId = transaction.SessionId,
                Amount = transaction.Amount,
                Duration = transaction.Duration,
                Status = transaction.Status.ToString(),
                CreatedAt = transaction.CreatedAt
            };

        public static IEnumerable<TransactionDto> ToDtos(
            this IEnumerable<Transaction> transactions)
            => transactions.Select(t => t.ToDto());
    }
}