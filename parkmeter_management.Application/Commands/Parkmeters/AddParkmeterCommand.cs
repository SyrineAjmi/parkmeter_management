using MediatR;

namespace parkmeter_management.Application.Commands.Parkmeters
{
    public class AddParkmeterCommand : IRequest<Guid>
    {
        public string SerialNumber { get; set; } = string.Empty;
        public Guid SiteId { get; set; }
    }
}