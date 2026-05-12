using MediatR;
using parkmeter_management.Application.Commands.Parkmeters;
using parkmeter_management.Application.Managers.Interfaces;
using parkmeter_management.Application.Queries.Parkmeters;
using parkmeter_management.Domain.Entities;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Application.Managers.Implementations
{
    public class ParkmeterManager : IParkmeterManager
    {
        private readonly IMediator _mediator;

        public ParkmeterManager(
            IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task<Guid> AddParkmeter(string serialNumber, Guid siteId)
            => await _mediator.Send(new AddParkmeterCommand
            {
                SerialNumber = serialNumber,
                SiteId = siteId
            });

        public async Task<bool> AssignToSite(Guid parkmeterId, Guid siteId)
               => await _mediator.Send(new AssignSiteCommand
               {
                   ParkmeterId = parkmeterId,
                   SiteId = siteId
               });


        public async Task<List<Parkmeter>> GetBySite(Guid siteId)
       => await _mediator.Send(new GetParkmetersBySiteQuery
       {
           SiteId = siteId
       });


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



        public async Task<List<Parkmeter>> FilterByStatus(MeterStatus status)
            => await _mediator.Send(
                new GetParkmetersByStatusQuery { Status = status });
    }
}