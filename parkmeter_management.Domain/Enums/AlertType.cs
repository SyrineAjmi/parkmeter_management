using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace parkmeter_management.Domain.Enums
{
    [Flags]
    public enum AlertType
    {
        NONE = 0,
        CASH_BOX_WARN = 1,
        CASH_BOX_FULL = 2,
        CASH_BOX_REMOVED = 4,
        PAPER_LOW = 8,
        PAPER_EMPTY = 16,
        PAPER_REFILLED = 32,
        HARDWARE_FAILURE = 64,
        DOOR_OPEN = 128,
        POWER_FAILURE = 256,
        CONNECTION_LOST = 512
    }
}
