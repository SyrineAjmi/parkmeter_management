using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Application.Managers.Interfaces;
using parkmeter_management.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace parkmeter_management.Application.Managers.Implementations
{
    public class MachineEventManager : IMachineEventManager
    {
        private readonly IMachineEventRepository _repo;

        public MachineEventManager(IMachineEventRepository repo)
            => _repo = repo;

        public async Task<List<MachineEvent>> GetAllEvents()
            => (await _repo.GetAllAsync()).ToList();

        public async Task<List<MachineEvent>> GetByParkmeter(Guid parkmeterID)
            => (await _repo.GetByParkmeterAsync(parkmeterID)).ToList();
    }
}
