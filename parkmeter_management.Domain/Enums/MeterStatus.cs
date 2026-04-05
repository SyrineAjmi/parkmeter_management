using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace parkmeter_management.Domain.Enums
{
    public enum MeterStatus { 
                                IN_SERVICE, 
                                OUT_OF_SERVICE, 
                                MAINTENANCE,
                                TEMPORARILY_UNAVAILABLE
    }

}
