using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace parkmeter_management.Application.Queries.Stats.CurrentSession
{
    public record CurrentSessionResult(
        Guid ParkmeterID,
        string SerialNumber,
        DateTime SessionStart,
        int TotalTransactions,
        decimal TotalAmount,
        string Status);

    public class GetCurrentSessionQuery
        : IRequest<CurrentSessionResult?>
    {
        public Guid ParkmeterID { get; set; }
    }
}
