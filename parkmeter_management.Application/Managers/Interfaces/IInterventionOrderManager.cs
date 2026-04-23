using parkmeter_management.Domain.Entities;
using parkmeter_management.Domain.Enums;

namespace parkmeter_management.Application
    .Managers.Interfaces
{
    public interface IInterventionOrderManager
    {
        Task<Guid> CreateOrder(Guid parkmeterID, Guid operatorId, InterventionType type);
        Task<List<InterventionOrder>> GetAllOrders();
        Task<List<InterventionOrder>> GetByParkmeter(
            Guid parkmeterID);
        Task<List<InterventionOrder>> GetPendingOrders();
        Task<List<InterventionOrder>> GetByOperator(Guid operatorId); 

    }
}