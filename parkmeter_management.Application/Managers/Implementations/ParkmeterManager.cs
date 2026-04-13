using MediatR;
using parkmeter_management.Application.Commands.Parkmeters;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Application.Managers.Interfaces;
using parkmeter_management.Application.Queries.Parkmeters;
using parkmeter_management.Domain.Entities;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Application.Managers.Implementations
{
    public class ParkmeterManager : IParkmeterManager
    {
        private readonly IMediator _mediator;
        private readonly IParkmeterRepository _parkmeterRepository;

        public ParkmeterManager(
            IMediator mediator,
            IParkmeterRepository parkmeterRepository)
        {
            _mediator = mediator;
            _parkmeterRepository = parkmeterRepository;
        }

        public async Task<Guid> AddParkmeter(string serialNumber, Guid siteId)
            => await _mediator.Send(new AddParkmeterCommand
            {
                SerialNumber = serialNumber,
                SiteId = siteId
            });

        public async Task<bool> AssignToSite(Guid parkmeterId, Guid siteId)
        {
            var parkmeter = await _parkmeterRepository
                .GetByIdAsync(parkmeterId);
            if (parkmeter == null) return false;

            parkmeter.SiteId = siteId;
            await _parkmeterRepository.UpdateAsync(parkmeter);
            return true;
        }

        public async Task<bool> ChangeStatus(
            Guid parkmeterId, MeterStatus newStatus)
            => await _mediator.Send(new ChangeStatusCommand
            {
                ParkmeterId = parkmeterId,
                NewStatus = newStatus
            });

        public async Task<List<Parkmeter>> GetAllParkmeters()
            => await _mediator.Send(new GetAllParkmetersQuery());

        public async Task<Parkmeter?> GetById(Guid id)
            => await _mediator.Send(new GetParkmeterByIdQuery { Id = id });

        public async Task<List<Parkmeter>> GetBySite(Guid siteId)
        {
            var parkmeters = await _parkmeterRepository
                .GetBySiteAsync(siteId);
            return parkmeters.ToList();
        }

        public async Task<List<Parkmeter>> FilterByStatus(MeterStatus status)
            => await _mediator.Send(
                new GetParkmetersByStatusQuery { Status = status });
    }
}