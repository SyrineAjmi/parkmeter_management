using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace parkmeter_management.Application.Commands.MachineEvents.HandleParkmeterOffline
{
    public class HandleParkmeterOfflineCommand: IRequest<bool>
    {
        public Guid ParkmeterId { get; set; }
    }

}
