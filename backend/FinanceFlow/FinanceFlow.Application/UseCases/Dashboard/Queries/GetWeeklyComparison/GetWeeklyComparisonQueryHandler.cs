using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Dashboard.Queries.GetWeeklyComparison;

public class GetWeeklyComparisonQueryHandler(
    ITransactionRepository transactionRepository,
    ICacheService cache)
    : IRequestHandler<GetWeeklyComparisonQuery, IEnumerable<WeeklyComparisonDto>>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public async Task<IEnumerable<WeeklyComparisonDto>> Handle(
        GetWeeklyComparisonQuery request,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"dashboard:weekly-comparison:{request.UserId}:{request.Year}:{request.Month}";

        return await cache.GetOrSetAsync(cacheKey, async () =>
        {
            var dateFrom = new DateTime(request.Year, request.Month, 1);
            var dateTo = dateFrom.AddMonths(1).AddDays(-1);

            var (transactions, _) = await transactionRepository.GetPagedByUserAsync(
                userId: request.UserId,
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

            var daysInMonth = DateTime.DaysInMonth(request.Year, request.Month);

            var weeks = new[]
            {
                (Week: 1, Label: $"Sem 1 ({request.Month:00}/01-07)",                              From: 1,  To: 7),
                (Week: 2, Label: $"Sem 2 ({request.Month:00}/08-14)",                              From: 8,  To: 14),
                (Week: 3, Label: $"Sem 3 ({request.Month:00}/15-21)",                              From: 15, To: 21),
                (Week: 4, Label: $"Sem 4 ({request.Month:00}/22-{daysInMonth:00})", From: 22, To: daysInMonth),
            };

            return weeks.Select(w =>
            {
                var weekTx = filtered
                    .Where(t => t.Date.Day >= w.From && t.Date.Day <= w.To)
                    .ToList();

                return new WeeklyComparisonDto(
                    Week: w.Week,
                    Label: w.Label,
                    Income: weekTx.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount),
                    Expenses: weekTx.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount));
            });

        }, CacheTtl, cancellationToken);
    }
}
