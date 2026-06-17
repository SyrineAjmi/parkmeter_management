using Microsoft.Extensions.Configuration;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Application.Interfaces.Services;
using parkmeter_management.Domain.Enums;
using System.Net.Http.Json;
using System.Text.Json;

namespace parkmeter_management.Infrastructure.AI
{
    public class AiReportService : IAiReportService
    {
        private readonly HttpClient _httpClient;
        private readonly ITransactionRepository _transactionRepo;
        private readonly IAlertRepository _alertRepo;
        private readonly IParkmeterRepository _parkmeterRepo;
        private readonly IConfiguration _config;

        public AiReportService(
            IHttpClientFactory httpClientFactory,
            ITransactionRepository transactionRepo,
            IAlertRepository alertRepo,
            IParkmeterRepository parkmeterRepo,
            IConfiguration config)
        {
            _httpClient = httpClientFactory.CreateClient("Gemini");
            _transactionRepo = transactionRepo;
            _alertRepo = alertRepo;
            _parkmeterRepo = parkmeterRepo;
            _config = config;
        }

        public async Task<AiReportResult> GenerateFinancialReportAsync(int month, int year)
        {
            var parkmeters = await _parkmeterRepo.GetAllAsync();
            var transactions = await _transactionRepo.GetByPeriodAsync(month, year);

            var pmStats = parkmeters.Select(pm =>
            {
                var pmTx = transactions.Where(t => t.ParkmeterId == pm.Id).ToList();
                var completed = pmTx.Where(t => t.Status == TransactionStatus.COMPLETED).ToList();

                return new
                {
                    SerialNumber = pm.SerialNumber,
                    Site = pm.Site?.Name ?? "—",
                    RevenuTotal = completed.Sum(t => t.Amount),
                    NombreTransactions = pmTx.Count,
                    TransactionsReussies = completed.Count,
                    TauxReussite = pmTx.Count > 0
                        ? Math.Round((double)completed.Count / pmTx.Count * 100, 1)
                        : 0,
                    MontantMoyen = completed.Count > 0
                        ? Math.Round(completed.Average(t => (double)t.Amount), 2)
                        : 0
                };
            }).ToList();

            var totalRevenue = pmStats.Sum(s => s.RevenuTotal);
            var totalTransactions = pmStats.Sum(s => s.NombreTransactions);
            var successRate = pmStats.Count > 0
                ? Math.Round(pmStats.Average(s => s.TauxReussite), 1)
                : 0;

            var serializedStats = JsonSerializer.Serialize(
                pmStats,
                new JsonSerializerOptions { WriteIndented = true }
            );

            var prompt = $@"
Tu es un expert en analyse financière d'un système de parcmètres urbains à Tunis.

Génère un rapport financier mensuel professionnel pour {month:D2}/{year}.

════════ DONNÉES GLOBALES ════════
Revenu total: {totalRevenue:F2} DT
Nombre total de transactions: {totalTransactions}
Taux de réussite moyen: {successRate}%

════════ DÉTAIL PAR PARCMÈTRE ════════
{serializedStats}

════════ INSTRUCTIONS ════════
Génère un rapport structuré en français avec :

1. Résumé exécutif
- 3 à 4 phrases maximum
- performance globale + revenu total

2. Analyse financière
- parcomètres les plus rentables
- variations et tendances

3. Performance des transactions
- taux de réussite global
- problèmes éventuels

4. Insights financiers
- observation des patterns
- efficacité globale du système

5. Conclusion
- synthèse + recommandation financière

Style: professionnel, clair, basé uniquement sur les données.";

            var apiKey = _config["Gemini:ApiKey"];
            var url = $"/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";

            var requestBody = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                }
            };

            if (totalTransactions == 0)
            {
                return new AiReportResult
                {
                    Period = $"{month:D2}/{year}",
                    Content = $"Aucune transaction enregistrée pour la période {month:D2}/{year}. " +
                              $"Le rapport ne peut pas être généré faute de données suffisantes.",
                    TotalRevenue = 0,
                    TotalTransactions = 0,
                    GeneratedAt = DateTime.UtcNow
                };
            }

            try
            {
                var response = await _httpClient.PostAsJsonAsync(url, requestBody);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<JsonElement>();

                var reportContent = result
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString()
                    ?? "Rapport non disponible";

                return new AiReportResult
                {
                    Period = $"{month:D2}/{year}",
                    Content = reportContent,
                    TotalRevenue = totalRevenue,
                    TotalTransactions = totalTransactions,
                    GeneratedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AI] Error: {ex.Message}");
                return new AiReportResult
                {
                    Period = $"{month:D2}/{year}",
                    Content = "Une erreur est survenue lors de la génération du rapport. " +
                              "Veuillez réessayer ultérieurement.",
                    TotalTransactions = totalTransactions,
                    GeneratedAt = DateTime.UtcNow
                };
            }
        }
    }

}