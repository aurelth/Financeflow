using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Application.DTOs.Reports;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Analytics.Queries.GetAnnualSummary;

public class GetAnnualSummaryQueryHandler(
    ITransactionRepository transactionRepository,
    ICacheService cache)
    : IRequestHandler<GetAnnualSummaryQuery, AnnualSummaryDto>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    private static readonly string[] MonthNames =
    [
        "Janeiro", "Fevereiro", "Março", "Abril", "Maio", "Junho",
        "Julho", "Agosto", "Setembro", "Outubro", "Novembro", "Dezembro"
    ];

    public async Task<AnnualSummaryDto> Handle(
        GetAnnualSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"analytics:annual:{request.UserId}:{request.Year}";

        return await cache.GetOrSetAsync(cacheKey, async () =>
        {
            var from = new DateTime(request.Year, 1, 1);
            var to = new DateTime(request.Year, 12, 31);

            var (transactions, _) = await transactionRepository.GetPagedByUserAsync(
                userId: request.UserId,
                page: 1,
                pageSize: int.MaxValue,
                dateFrom: from,
                dateTo: to,
                categoryId: null,
                subcategoryId: null,
                type: null,
                status: null,
                amountMin: null,
                amountMax: null,
                search: null,
                cancellationToken: cancellationToken);

            // Filtra apenas transações confirmadas
            var confirmed = transactions
                .Where(t => t.Status != TransactionStatus.Scheduled)
                .ToList();

            var months = new List<AnnualMonthDto>();
            var cumulative = 0m;

            for (var month = 1; month <= 12; month++)
            {
                var monthTx = confirmed
                    .Where(t => t.Date.Month == month)
                    .ToList();

                var income = monthTx.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
                var expenses = monthTx.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
                var balance = income - expenses;
                cumulative += balance;

                months.Add(new AnnualMonthDto(
                    Month: month,
                    MonthName: MonthNames[month - 1],
                    Income: income,
                    Expenses: expenses,
                    Balance: balance,
                    CumulativeBalance: cumulative));
            }

            var totalIncome = months.Sum(m => m.Income);
            var totalExpenses = months.Sum(m => m.Expenses);

            // Considera apenas meses com movimento para a média
            var activeMonths = months.Count(m => m.Income > 0 || m.Expenses > 0);
            var divisor = activeMonths > 0 ? activeMonths : 1;

            return new AnnualSummaryDto(
                Year: request.Year,
                Months: months,
                TotalIncome: totalIncome,
                TotalExpenses: totalExpenses,
                NetBalance: totalIncome - totalExpenses,
                AverageMonthlyIncome: Math.Round(totalIncome / divisor, 2),
                AverageMonthlyExpenses: Math.Round(totalExpenses / divisor, 2));

        }, CacheTtl, cancellationToken);
    }
}
