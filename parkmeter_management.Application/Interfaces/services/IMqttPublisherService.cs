using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Application.Interfaces.Services
{
    public interface IMqttPublisherService
    {
        Task PublishConfigAsync(
            Guid parkmeterId,
            decimal pricePerHour,
            int maxDuration);

        Task PublishStatusAsync(
            Guid parkmeterId,
            string status);

        Task PublishNewParkmeterAsync(
            Guid parkmeterID,
            string serialNumber);

        /// <summary>
        /// Publishes an operator/lifecycle event on behalf of the simulator
        /// to parkmeters/{id}/events — same topic real hardware would use.
        /// </summary>
        Task PublishEventAsync(
            Guid parkmeterId,
            ParkMeterEventType eventType);
    }
}