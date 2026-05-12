using parkmeter_management.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace parkmeter_management.Application.Managers.Interfaces
{
    public interface IMachineEventManager
    {
        Task<List<MachineEvent>> GetAllEvents();
        Task<List<MachineEvent>> GetByParkmeter(Guid parkmeterID);
    }
}
