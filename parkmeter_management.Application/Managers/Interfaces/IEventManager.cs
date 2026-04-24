using parkmeter_management.Domain.Entities;

public interface IEventManager
{
    Task<List<ParkMeterEvent>> GetRecentByParkmeter(
        Guid parkmeterID, int count = 20);
}