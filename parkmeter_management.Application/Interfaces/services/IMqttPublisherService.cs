namespace parkmeter_management.Application.Interfaces.Services
{
    public interface IMqttPublisherService
    {
        Task PublishConfigAsync(Guid parkmeterId, decimal pricePerHour, int maxDuration);
        Task PublishStatusAsync(Guid parkmeterId, string status);
        Task PublishNewParkmeterAsync(Guid parkmeterID, string serialNumber);
    }
}