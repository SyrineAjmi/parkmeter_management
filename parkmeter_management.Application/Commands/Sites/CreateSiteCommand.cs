using MediatR;

namespace parkmeter_management.Application.Commands.Sites
{
    public class CreateSiteCommand : IRequest<Guid>
    {
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }

    }
}