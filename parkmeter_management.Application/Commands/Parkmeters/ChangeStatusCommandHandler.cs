using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Application.Interfaces.Services;

namespace parkmeter_management.Application.Commands.Parkmeters
{
    public class ChangeStatusCommandHandler
        : IRequestHandler<ChangeStatusCommand, bool>
    {
        private readonly IParkmeterRepository _parkmeterRepository;
        private readonly IMessagePublisherService _mqttPublisher;

        public ChangeStatusCommandHandler(
            IParkmeterRepository parkmeterRepository,
            IMessagePublisherService mqttPublisher)        
        {
            _parkmeterRepository = parkmeterRepository;
            _mqttPublisher = mqttPublisher;
        }

        public async Task<bool> Handle(
            ChangeStatusCommand command, CancellationToken ct)
        {
            var parkmeter = await _parkmeterRepository
                .GetByIdAsync(command.ParkmeterId);
            if (parkmeter == null) return false;


            var published = await _mqttPublisher.PublishCommandAsync(
                command.ParkmeterId,
                $"SET_{command.NewStatus}");

            if (!published) return false;

            Console.WriteLine(
                $"[COMMAND] SET_{command.NewStatus} sent " +
                $"→ {command.ParkmeterId}");

            return true;
        }
    }
}
