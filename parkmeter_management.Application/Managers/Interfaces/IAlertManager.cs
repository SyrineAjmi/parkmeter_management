using parkmeter_management.Application.Commands.Alerts;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Application.Managers.Interfaces
{
    public interface IAlertManager
    {
        Task<Guid> RecordAlert(CreateAlertCommand command);
        Task<bool> ResolveAlert(Guid alertId);
        Task<List<Alert>> GetAllAlerts();
        Task<Alert?> GetById(Guid id);
        Task<List<Alert>> FilterByPriority(string priority);
        Task<List<Alert>> GetUnresolvedAlerts();        
        Task<List<Alert>> GetByParkmeter(Guid parkmeterID);
        Task<List<Alert>> GetUnresolvedByParkmeter(Guid parkmeterID);
    }
}