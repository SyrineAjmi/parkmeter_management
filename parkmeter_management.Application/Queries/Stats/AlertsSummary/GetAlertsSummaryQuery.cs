using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace parkmeter_management.Application.Queries.Stats.AlertsSummary
{
    public record AlertsSummaryResult(
         int TotalUnresolved,
         int High,
         int Normal,
         int Low,
         Dictionary<string, int> ByType);

    public class GetAlertsSummaryQuery
        : IRequest<AlertsSummaryResult>
    { }
}
