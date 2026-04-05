using MediatR;

namespace parkmeter_management.Application.Commands.ParkmeterConfiguration
{
    public class SendParkConfigCommand : IRequest<bool>
    {
        public Guid ParkmeterId { get; set; }
        public decimal PricePerHour { get; set; }
        public int MaxDuration { get; set; }
    }
}