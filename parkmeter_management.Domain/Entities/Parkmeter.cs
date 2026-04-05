using parkmeter_management.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace parkmeter_management.Domain.Entities
{
    public class Parkmeter
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string SerialNumber { get; set; } = string.Empty;
        public Guid SiteId { get; set; }
        public MeterStatus Status { get; set; } = MeterStatus.IN_SERVICE;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Site? Site { get; set; }
        public ParkMeterConfig? Configuration { get; set; }
    }

}
