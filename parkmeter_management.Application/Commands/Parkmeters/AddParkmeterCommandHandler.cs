using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Application.Interfaces.Services;
using parkmeter_management.Domain.Entities;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Application.Commands.Parkmeters
{
    public class AddParkmeterCommandHandler
        : IRequestHandler<AddParkmeterCommand, Guid>
    {
        private readonly IParkmeterRepository _parkmeterRepository;
        private readonly ISiteRepository _siteRepository;
        private readonly IMqttPublisherService _mqttPublisher; 


        public AddParkmeterCommandHandler(
            IParkmeterRepository parkmeterRepository,
            ISiteRepository siteRepository,
            IMqttPublisherService mqttPublisher)
        {
            _parkmeterRepository = parkmeterRepository;
            _siteRepository = siteRepository;
            _mqttPublisher = mqttPublisher; 
        }

    public async Task<Guid> Handle(
    AddParkmeterCommand command, CancellationToken ct)
{
    // chek site exists
    var site = await _siteRepository.GetByIdAsync(command.SiteId);
    if (site == null)
        throw new Exception($"Site {command.SiteId} not found.");

    // check serial number is unique
    var existing = await _parkmeterRepository
        .GetBySerialNumberAsync(command.SerialNumber);
    if (existing != null)
        throw new Exception(
            $"Parkmeter with serial number '{command.SerialNumber}' already exists.");

    var parkmeter = new Parkmeter
    {
        Id           = Guid.NewGuid(),
        SerialNumber = command.SerialNumber,
        SiteId       = command.SiteId,
        Status       = MeterStatus.PENDING_ACTIVATION,
        CreatedAt    = DateTime.UtcNow
    };

    await _parkmeterRepository.AddAsync(parkmeter);
    await _mqttPublisher.PublishNewParkmeterAsync(
           parkmeter.Id,
           parkmeter.SerialNumber);

        return parkmeter.Id;
}
    }
}