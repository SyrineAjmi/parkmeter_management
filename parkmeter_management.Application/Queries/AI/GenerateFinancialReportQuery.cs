using MediatR;
using parkmeter_management.Application.Interfaces.Services;

namespace parkmeter_management.Application.Queries.AI
{
    public class GenerateFinancialReportQuery : IRequest<AiReportResult>
    {
        public int Month { get; set; } = DateTime.UtcNow.Month;
        public int Year { get; set; } = DateTime.UtcNow.Year;
    }

    public class GenerateFinancialReportQueryHandler
        : IRequestHandler<GenerateFinancialReportQuery, AiReportResult>
    {
        private readonly IAiReportService _aiService;

        public GenerateFinancialReportQueryHandler(IAiReportService aiService)
        {
            _aiService = aiService;
        }

        public async Task<AiReportResult> Handle(
            GenerateFinancialReportQuery request,
            CancellationToken ct)
        {
            return await _aiService.GenerateFinancialReportAsync(
                request.Month,
                request.Year);
        }
    }
}