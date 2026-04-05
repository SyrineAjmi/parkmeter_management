using parkmeter_management.Domain.Entities;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Application.Managers.Interfaces
{
    public interface IParkmeterManager
    {
        Task<Guid> AddParkmeter(string serialNumber, Guid siteId);
        Task<bool> AssignToSite(Guid parkmeterId, Guid siteId);
        Task<bool> ChangeStatus(Guid parkmeterId, MeterStatus newStatus);
        Task<List<Parkmeter>> GetAllParkmeters();
        Task<Parkmeter?> GetById(Guid id);
        Task<List<Parkmeter>> GetBySite(Guid siteId);
    }
}