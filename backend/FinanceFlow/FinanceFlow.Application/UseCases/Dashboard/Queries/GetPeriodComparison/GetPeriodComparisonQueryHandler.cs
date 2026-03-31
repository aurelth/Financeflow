using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Dashboard.Queries.GetPeriodComparison;

public class GetPeriodComparisonQueryHandler(
    ITransactionRepository transactionRepository,
    ICacheService cache)
    : IRequestHandler<GetPeriodComparisonQuery, PeriodComparisonDto>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public async Task<PeriodComparisonDto> Handle(
        GetPeriodComparisonQuery request,
        CancellationToken cancellationToken)
    {
        var periods = request.Periods.Take(3).ToList();
        var periodsKey = string.Join("_", periods.Select(p => $"{p.Year}-{p.Month:00}"));
        var cacheKey = $"dashboard:period-comparison:{request.UserId}:{periodsKey}";

        return await cache.GetOrSetAsync(cacheKey, async () =>
        {
            // Execução sequencial para evitar conflito no DbContext
            var results = new List<PeriodResult>();
            foreach (var p in periods)
            {
                var result = await FetchPeriodDataAsync(
                    request.UserId, p.Month, p.Year, cancellationToken);
                results.Add(result);
            }

            // Monta os dados por período
            var periodDtos = results.Select(r => new PeriodDataDto(
                Month: r.Month,
                Year: r.Year,
                TotalIncome: r.Income,
                TotalExpenses: r.Expenses,
                Balance: r.Income - r.Expenses));

            // Agrupa todas as categorias únicas
            var allCategoryIds = results
                .SelectMany(r => r.ByCategory.Keys)
                .Distinct()
                .ToList();

            // Monta comparações por categoria
            var categoryComparisons = allCategoryIds.Select(categoryId =>
            {
                var first = results
                    .SelectMany(r => r.ByCategory)
                    .FirstOrDefault(x => x.Key == categoryId).Value;

                var values = results
                    .Select(r => r.ByCategory.TryGetValue(categoryId, out var val) ? val.Total : 0m)
                    .ToArray();

                var variations = new decimal?[values.Length];
                variations[0] = null;

                for (var i = 1; i < values.Length; i++)
                {
                    variations[i] = values[i - 1] == 0
                        ? null
                        : Math.Round((values[i] - values[i - 1]) / values[i - 1] * 100, 2);
                }

                return new CategoryComparisonDto(
                    CategoryId: categoryId.ToString(),
                    CategoryName: first?.Name ?? string.Empty,
                    CategoryIcon: first?.Icon ?? string.Empty,
                    CategoryColor: first?.Color ?? string.Empty,
                    Values: values,
                    Variations: variations);
            })
            .OrderByDescending(c => c.Values.Max())
            .ToList();

            return new PeriodComparisonDto(
                Periods: periodDtos,
                CategoryComparisons: categoryComparisons);

        }, CacheTtl, cancellationToken);
    }

    private async Task<PeriodResult> FetchPeriodDataAsync(
    Guid userId,
    int month,
    int year,
    CancellationToken cancellationToken)
    {
        var dateFrom = new DateTime(year, month, 1);
        var dateTo = dateFrom.AddMonths(1).AddDays(-1);

        var (transactions, _) = await transactionRepository.GetPagedByUserAsync(
            userId: userId,
            page: 1,
            pageSize: int.MaxValue,
            dateFrom: dateFrom,
            dateTo: dateTo,
            categoryId: null,
            subcategoryId: null,
            type: null,
            status: null,
            amountMin: null,
            amountMax: null,
            search: null,
            cancellationToken: cancellationToken);

        var filtered = transactions
            .Where(t => t.Status != TransactionStatus.Scheduled)
            .ToList();

        var income = filtered.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
        var expenses = filtered.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);

        var byCategory = filtered
            .Where(t => t.Type == TransactionType.Expense)
            .GroupBy(t => t.CategoryId)
            .ToDictionary(
                g => g.Key,
                g => new CategoryInfo(
                    Name: g.First().Category.Name,
                    Icon: g.First().Category.Icon,
                    Color: g.First().Category.Color,
                    Total: g.Sum(t => t.Amount)));

        return new PeriodResult(month, year, income, expenses, byCategory);
    }

    private record CategoryInfo(string Name, string Icon, string Color, decimal Total);

    private record PeriodResult(
        int Month,
        int Year,
        decimal Income,
        decimal Expenses,
        Dictionary<Guid, CategoryInfo> ByCategory);
}
