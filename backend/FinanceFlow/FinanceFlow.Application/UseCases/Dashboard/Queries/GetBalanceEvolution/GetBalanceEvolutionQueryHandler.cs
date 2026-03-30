using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Dashboard.Queries.GetBalanceEvolution;

public class GetBalanceEvolutionQueryHandler(
    ITransactionRepository transactionRepository)
    : IRequestHandler<GetBalanceEvolutionQuery, IEnumerable<BalanceEvolutionDto>>
{
    public async Task<IEnumerable<BalanceEvolutionDto>> Handle(
        GetBalanceEvolutionQuery request,
        CancellationToken cancellationToken)
    {
        var dateFrom = new DateTime(request.Year, request.Month, 1);
        var dateTo = dateFrom.AddMonths(1).AddDays(-1);

        // Limita ao dia atual se for o mês corrente
        var today = DateTime.UtcNow.Date;
        if (dateTo > today) dateTo = today;

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

        // Agrupa por dia
        var byDay = transactions
            .Where(t => t.Status != TransactionStatus.Scheduled)
            .GroupBy(t => t.Date.Date)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    Income = g.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount),
                    Expenses = g.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount),
                });

        var result = new List<BalanceEvolutionDto>();
        decimal accumulated = 0;

        for (var day = dateFrom.Date; day <= dateTo; day = day.AddDays(1))
        {
            var income = byDay.TryGetValue(day, out var d) ? d.Income : 0;
            var expenses = byDay.TryGetValue(day, out var d2) ? d2.Expenses : 0;

            accumulated += income - expenses;

            result.Add(new BalanceEvolutionDto(
                Date: day.ToString("yyyy-MM-dd"),
                Income: income,
                Expenses: expenses,
                Balance: accumulated));
        }

        return result;
    }
}
