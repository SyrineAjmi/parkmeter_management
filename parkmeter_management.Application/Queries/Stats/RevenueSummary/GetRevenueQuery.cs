using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace parkmeter_management.Application.Queries.Stats.RevenueSummary
{
    public record RevenueSummaryResult(
          decimal TotalRevenue,
          int TotalTransactions,
          int CompletedTransactions,
          int FailedTransactions,
          int CancelledTransactions,
          int ExpiredTransactions,
          DateTime From,
          DateTime To);

    public class GetRevenueQuery : IRequest<RevenueSummaryResult>
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
    }
}
