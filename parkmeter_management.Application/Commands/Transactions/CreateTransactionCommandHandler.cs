using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Application.Interfaces.Services;
using parkmeter_management.Domain.Entities;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Application.Commands.Transactions
{
    public class CreateTransactionCommandHandler
        : IRequestHandler<CreateTransactionCommand, Guid>
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly ISessionRepository _sessionRepository;
        private readonly IParkmeterRepository _parkmeterRepository;
        private readonly INotificationService _notificationService;

        public CreateTransactionCommandHandler(
            ITransactionRepository transactionRepository,
            ISessionRepository sessionRepository,
            IParkmeterRepository parkmeterRepository,
            INotificationService notificationService)
        {
            _transactionRepository = transactionRepository;
            _sessionRepository = sessionRepository;
            _parkmeterRepository = parkmeterRepository;
            _notificationService = notificationService;
        }

        public async Task<Guid> Handle(
            CreateTransactionCommand command, CancellationToken ct)
        {
            // check parkmeter exists + IN_SERVICE
            var parkmeter = await _parkmeterRepository
                .GetByIdAsync(command.ParkmeterId);

            if (parkmeter == null)
                return Guid.Empty;

            if (parkmeter.Status != MeterStatus.IN_SERVICE)
                return Guid.Empty;

            // get/create active session
            var activeSession = await _sessionRepository
                .GetActiveSessionByParkmeterAsync(command.ParkmeterId);

            if (activeSession == null)
            {
                activeSession = new Session(command.ParkmeterId);

                await _sessionRepository.AddAsync(activeSession);
            }

            // save transact
            var transaction = new Transaction(
                command.ParkmeterId,
                activeSession.Id,
                command.Amount,
                command.Duration,
                command.Status);

            await _transactionRepository.AddAsync(transaction);
            await _notificationService.NotifyNewTransaction(
                command.ParkmeterId,
                command.Amount,
                command.Status.ToString());

            // update session totals : COMPLETED only
            if (command.Status == TransactionStatus.COMPLETED)
            {
                activeSession.AddTransaction(command.Amount);
                await _sessionRepository.UpdateAsync(activeSession);
            }

            return transaction.Id;
        }
    }
}