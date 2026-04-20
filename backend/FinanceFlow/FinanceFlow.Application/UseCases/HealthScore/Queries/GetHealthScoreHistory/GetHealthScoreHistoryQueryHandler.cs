using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Application.DTOs.HealthScore;
using MediatR;

namespace FinanceFlow.Application.UseCases.HealthScore.Queries.GetHealthScoreHistory;

public class GetHealthScoreHistoryQueryHandler(IHealthScoreService healthScoreService)
    : IRequestHandler<GetHealthScoreHistoryQuery, IEnumerable<HealthScoreHistoryItem>>
{
    public async Task<IEnumerable<HealthScoreHistoryItem>> Handle(
        GetHealthScoreHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var result = new List<HealthScoreHistoryItem>();
        var now = DateTime.UtcNow;

        // Calcula os últimos 6 meses sequencialmente
        for (var i = 5; i >= 0; i--)
        {
            var date = now.AddMonths(-i);
            var month = date.Month;
            var year = date.Year;

            var score = await healthScoreService.CalculateAsync(
                request.UserId,
                month,
                year,
                cancellationToken);

            var monthLabel = new DateTime(year, month, 1)
                .ToString("MMM/yy", new System.Globalization.CultureInfo("pt-BR"));

            result.Add(new HealthScoreHistoryItem(
                Month: month,
                Year: year,
                MonthLabel: monthLabel,
                Score: score.Score,
                Classification: score.Classification));
        }

        return result;
    }
}
