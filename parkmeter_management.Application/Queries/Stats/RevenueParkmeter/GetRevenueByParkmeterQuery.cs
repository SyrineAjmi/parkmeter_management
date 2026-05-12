using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace parkmeter_management.Application.Queries.Stats.RevenueParkmeter
{
    public record RevenueByParkmeterResult(
            Guid ParkmeterID,
            string SerialNumber,
            decimal TotalRevenue,
            int TotalTransactions);

    public class GetRevenueByParkmeterQuery
        : IRequest<List<RevenueByParkmeterResult>>
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
    }
}
