using MediatR;

namespace parkmeter_management.Application.Commands.Parkmeters
{
    public class AssignSiteCommand : IRequest<bool>
    {
        public Guid ParkmeterId { get; set; }
        public Guid SiteId { get; set; }
    }
}