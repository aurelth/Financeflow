using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Application.DTOs.Reports;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using MediatR;
using System.Globalization;

namespace FinanceFlow.Application.UseCases.Analytics.Queries.GetCashFlow;

public class GetCashFlowQueryHandler(
    ITransactionRepository transactionRepository,
    ICacheService cache)
    : IRequestHandler<GetCashFlowQuery, CashFlowDto>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);   
    private static readonly CultureInfo PtBR = CultureInfo.GetCultureInfo("pt-BR");

    public async Task<CashFlowDto> Handle(
        GetCashFlowQuery request,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"analytics:cashflow:{request.UserId}:{request.From:yyyyMMdd}:{request.To:yyyyMMdd}:{request.GroupBy}";

        return await cache.GetOrSetAsync(cacheKey, async () =>
        {
            var (transactions, _) = await transactionRepository.GetPagedByUserAsync(
                userId: request.UserId,
                page: 1,
                pageSize: int.MaxValue,
                dateFrom: request.From,
                dateTo: request.To,
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

            var periods = request.GroupBy == "day"
                ? GroupByDay(confirmed, request.From, request.To)
                : GroupByMonth(confirmed, request.From, request.To);

            var totalIncome = confirmed.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
            var totalExpenses = confirmed.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);

            return new CashFlowDto(
                From: request.From,
                To: request.To,
                GroupBy: request.GroupBy,
                Periods: periods,
                TotalIncome: totalIncome,
                TotalExpenses: totalExpenses,
                NetBalance: totalIncome - totalExpenses);

        }, CacheTtl, cancellationToken);
    }

    private static List<CashFlowPeriodDto> GroupByDay(
        List<Transaction> transactions,
        DateTime from,
        DateTime to)
    {
        var periods = new List<CashFlowPeriodDto>();
        var cumulative = 0m;

        for (var date = from.Date; date <= to.Date; date = date.AddDays(1))
        {
            var dayTx = transactions.Where(t => t.Date.Date == date).ToList();
            var income = dayTx.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
            var expenses = dayTx.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
            var balance = income - expenses;
            cumulative += balance;

            periods.Add(new CashFlowPeriodDto(
                Label: date.ToString("dd/MM/yyyy", PtBR),
                Income: income,
                Expenses: expenses,
                Balance: balance,
                CumulativeBalance: cumulative));
        }

        return periods;
    }

    private static List<CashFlowPeriodDto> GroupByMonth(
        List<Transaction> transactions,
        DateTime from,
        DateTime to)
    {
        var periods = new List<CashFlowPeriodDto>();
        var cumulative = 0m;

        var current = new DateTime(from.Year, from.Month, 1);
        var end = new DateTime(to.Year, to.Month, 1);

        while (current <= end)
        {
            var monthTx = transactions
                .Where(t => t.Date.Year == current.Year && t.Date.Month == current.Month)
                .ToList();

            var income = monthTx.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
            var expenses = monthTx.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
            var balance = income - expenses;
            cumulative += balance;

            periods.Add(new CashFlowPeriodDto(
                Label: current.ToString("MMM/yyyy", PtBR),
                Income: income,
                Expenses: expenses,
                Balance: balance,
                CumulativeBalance: cumulative));

            current = current.AddMonths(1);
        }

        return periods;
    }
}
