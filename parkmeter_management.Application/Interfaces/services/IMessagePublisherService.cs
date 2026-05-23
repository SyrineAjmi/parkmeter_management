using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Application.Interfaces.Services
{
    public interface IMessagePublisherService
    {
        Task PublishConfigAsync(Guid parkmeterId, ParkMeterConfig config);
        Task PublishStatusAsync(Guid parkmeterId, string status);
        Task<bool> PublishCommandAsync(Guid parkmeterId, string command);

    }
}