using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Application.Managers.Interfaces
{
    public interface ISiteManager
    {
        Task<Guid> CreateSite(string name, string address,
            string description, double latitude, double longitude);
        Task<bool> UpdateSite(Guid id, string name,
            string address, string description, double latitude, double longitude);
        Task<bool> DeleteSite(Guid id);
        Task<List<Site>> GetAllSites();
        Task<Site?> GetById(Guid id);
    }
}