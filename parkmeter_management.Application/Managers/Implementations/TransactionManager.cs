using MediatR;
using parkmeter_management.Application.Commands.Transactions;
using parkmeter_management.Application.Managers.Interfaces;
using parkmeter_management.Application.Queries.Transactions;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Application.Managers.Implementations
{
    public class TransactionManager : ITransactionManager
    {
        private readonly IMediator _mediator;

        public TransactionManager(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task<Guid> RecordTransaction(
            CreateTransactionCommand command)
            => await _mediator.Send(command);

        public async Task<List<Transaction>> GetAllTransactions()
            => await _mediator.Send(new GetAllTransactionsQuery());

        public async Task<List<Transaction>> GetByParkmeter(Guid parkmeterId)
            => await _mediator.Send(
                new GetTransactionsByParkmeterQuery
                { ParkmeterId = parkmeterId });

        public async Task<List<Transaction>> FilterByStatus(string status)
            => await _mediator.Send(
                new GetTransactionsByStatusQuery { Status = status });

        public async Task<List<Transaction>> GetBySession(Guid sessionId)
            => await _mediator.Send(
                new GetTransactionsBySessionQuery
                { SessionId = sessionId });

        public async Task<List<Transaction>> GetByDateRange(
            DateTime? from, DateTime? to)
            => await _mediator.Send(
                new GetTransactionsByDateRangeQuery
                {
                    From = from,
                    To = to
                });

    }
}