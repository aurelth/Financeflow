using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Application.DTOs.Reports;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Analytics.Queries.GetProjections;

public class GetProjectionsQueryHandler(
    ITransactionRepository transactionRepository,
    ICacheService cache)
    : IRequestHandler<GetProjectionsQuery, ProjectionsDto>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    private static readonly string[] MonthNames =
    [
        "Janeiro", "Fevereiro", "Março", "Abril", "Maio", "Junho",
        "Julho", "Agosto", "Setembro", "Outubro", "Novembro", "Dezembro"
    ];

    public async Task<ProjectionsDto> Handle(
        GetProjectionsQuery request,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"analytics:projections:{request.UserId}:{request.MonthsBack}:{request.MonthsAhead}";

        return await cache.GetOrSetAsync(cacheKey, async () =>
        {
            var today = DateTime.UtcNow;
            var endDate = new DateTime(today.Year, today.Month, 1).AddDays(-1);
            var from = new DateTime(today.Year, today.Month, 1)
                .AddMonths(-request.MonthsBack);

            var (transactions, _) = await transactionRepository.GetPagedByUserAsync(
                userId: request.UserId,
                page: 1,
                pageSize: int.MaxValue,
                dateFrom: from,
                dateTo: endDate,
                categoryId: null,
                subcategoryId: null,
                type: null,
                status: null,
                amountMin: null,
                amountMax: null,
                search: null,
                cancellationToken: cancellationToken);

            var confirmed = transactions
                .Where(t => t.Status != TransactionStatus.Scheduled)
                .ToList();

            // Constrói dados históricos por mês
            var historicalData = new List<(int Year, int Month, decimal Income, decimal Expenses)>();

            for (var i = request.MonthsBack; i >= 1; i--)
            {
                var date = new DateTime(today.Year, today.Month, 1).AddMonths(-i);
                var monthTx = confirmed
                    .Where(t => t.Date.Year == date.Year && t.Date.Month == date.Month)
                    .ToList();

                historicalData.Add((
                    Year: date.Year,
                    Month: date.Month,
                    Income: monthTx.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount),
                    Expenses: monthTx.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount)
                ));
            }

            var historical = historicalData.Select(h => new ProjectionMonthDto(
                Year: h.Year,
                Month: h.Month,
                MonthName: MonthNames[h.Month - 1],
                Income: h.Income,
                Expenses: h.Expenses,
                Balance: h.Income - h.Expenses,
                IsProjected: false
            )).ToList();

            // Calcula projecções com tendência linear + sazonalidade + pesos
            var projected = CalculateProjections(historicalData, today, request.MonthsAhead);

            return new ProjectionsDto(
                MonthsAnalysed: request.MonthsBack,
                MonthsAhead: request.MonthsAhead,
                Historical: historical,
                Projected: projected);

        }, CacheTtl, cancellationToken);
    }

    private static List<ProjectionMonthDto> CalculateProjections(
        List<(int Year, int Month, decimal Income, decimal Expenses)> history,
        DateTime today,
        int monthsAhead)
    {
        var projected = new List<ProjectionMonthDto>();
        var n = history.Count;

        if (n == 0) return projected;

        // Pesos decrescentes — meses mais recentes têm mais peso
        var weights = Enumerable.Range(1, n)
            .Select(i => (double)i)
            .ToArray();
        var weightSum = weights.Sum();

        // Médias ponderadas de base
        var weightedIncome = history.Select((h, i) => (double)h.Income * weights[i]).Sum() / weightSum;
        var weightedExpenses = history.Select((h, i) => (double)h.Expenses * weights[i]).Sum() / weightSum;

        // Tendência linear (regressão simples ponderada)
        // x = índice do mês (0..n-1), y = valor
        var incomeSlope = CalculateWeightedSlope(history.Select(h => (double)h.Income).ToArray(), weights);
        var expensesSlope = CalculateWeightedSlope(history.Select(h => (double)h.Expenses).ToArray(), weights);

        for (var i = 1; i <= monthsAhead; i++)
        {
            var targetDate = new DateTime(today.Year, today.Month, 1).AddMonths(i);

            // Projecta com tendência
            var projectedIncome = weightedIncome + incomeSlope * i;
            var projectedExpenses = weightedExpenses + expensesSlope * i;

            // Ajuste sazonal — compara com o mesmo mês em anos anteriores no histórico
            var sameMonthHistory = history
                .Where(h => h.Month == targetDate.Month)
                .ToList();

            if (sameMonthHistory.Count > 0)
            {
                var seasonalIncome = (double)sameMonthHistory.Average(h => h.Income);
                var seasonalExpenses = (double)sameMonthHistory.Average(h => h.Expenses);

                // Factor sazonal = média do mesmo mês / média geral ponderada
                var incomeFactor = weightedIncome > 0 ? seasonalIncome / weightedIncome : 1;
                var expensesFactor = weightedExpenses > 0 ? seasonalExpenses / weightedExpenses : 1;

                // Combina tendência (70%) com sazonalidade (30%)
                projectedIncome = projectedIncome * 0.7 + projectedIncome * incomeFactor * 0.3;
                projectedExpenses = projectedExpenses * 0.7 + projectedExpenses * expensesFactor * 0.3;
            }

            // Garante valores não negativos
            projectedIncome = Math.Max(0, projectedIncome);
            projectedExpenses = Math.Max(0, projectedExpenses);

            projected.Add(new ProjectionMonthDto(
                Year: targetDate.Year,
                Month: targetDate.Month,
                MonthName: MonthNames[targetDate.Month - 1],
                Income: Math.Round((decimal)projectedIncome, 2),
                Expenses: Math.Round((decimal)projectedExpenses, 2),
                Balance: Math.Round((decimal)(projectedIncome - projectedExpenses), 2),
                IsProjected: true));
        }

        return projected;
    }

    private static double CalculateWeightedSlope(double[] values, double[] weights)
    {
        var n = values.Length;
        var weightSum = weights.Sum();

        // Média ponderada de x e y
        var xValues = Enumerable.Range(0, n).Select(i => (double)i).ToArray();
        var weightedXm = xValues.Select((x, i) => x * weights[i]).Sum() / weightSum;
        var weightedYm = values.Select((y, i) => y * weights[i]).Sum() / weightSum;

        // Numerador e denominador da regressão ponderada
        var numerator = xValues.Select((x, i) => weights[i] * (x - weightedXm) * (values[i] - weightedYm)).Sum();
        var denominator = xValues.Select((x, i) => weights[i] * Math.Pow(x - weightedXm, 2)).Sum();

        return denominator > 0 ? numerator / denominator : 0;
    }
}
