using MediatR;
using parkmeter_management.Application.Commands.Transactions;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Application.Interfaces.Services;
using parkmeter_management.Domain.Entities;
using parkmeter_management.Domain.Enums;
using static System.Collections.Specialized.BitVector32;

namespace parkmeter_management.Application.Commands.Transactions
{
    public class CreateTransactionCommandHandler
        : IRequestHandler<CreateTransactionCommand, Guid>
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly ISessionRepository _sessionRepository;
        private readonly INotificationService _notificationService;


        public CreateTransactionCommandHandler(
            ITransactionRepository transactionRepository,
            ISessionRepository sessionRepository,
            INotificationService notificationService)

        {
            _transactionRepository = transactionRepository;
            _sessionRepository = sessionRepository;
            _notificationService = notificationService;
        }
        
        public async Task<Guid> Handle(
            CreateTransactionCommand command, CancellationToken ct)
        {
            // get active session from DB
            var activeSession = await _sessionRepository
                .GetActiveSessionByParkmeterAsync(command.ParkmeterId);

            if (activeSession == null)
            {
                // if no active sess create one
                activeSession = new Session
                {
                    Id = Guid.NewGuid(),
                    ParkmeterId = command.ParkmeterId,
                    StartDateTime = DateTime.UtcNow,
                    Status = SessionStatus.ACTIVE,
                    TotalTransactions = 0,
                    TotalAmount = 0
                };
                await _sessionRepository.AddAsync(activeSession);
            }

            // save transaction with current session id
            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                ParkmeterId = command.ParkmeterId,
                SessionId = activeSession.Id,
                Amount = command.Amount,
                Duration = command.Duration,
                Status = command.Status,
                CreatedAt = DateTime.UtcNow
            };

            await _transactionRepository.AddAsync(transaction);
            await _notificationService.NotifyNewTransaction(
                        command.ParkmeterId,
                        command.Amount,
                        command.Status.ToString());

            // only update session totals for COMPLETED transactions
            if (command.Status == TransactionStatus.COMPLETED)
            {
                activeSession.TotalTransactions++;
                activeSession.TotalAmount += command.Amount;
                await _sessionRepository.UpdateAsync(activeSession);
            }



            return transaction.Id;
        }

    }
}
