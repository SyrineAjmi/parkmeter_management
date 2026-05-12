using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace parkmeter_management.Application.Queries.Stats.RevenueSite
{
    public record RevenueBySiteResult(
        Guid SiteId,
        string SiteName,
        decimal TotalRevenue,
        int TotalTransactions);

    public class GetRevenueBySiteQuery
        : IRequest<List<RevenueBySiteResult>>
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
    }
}
