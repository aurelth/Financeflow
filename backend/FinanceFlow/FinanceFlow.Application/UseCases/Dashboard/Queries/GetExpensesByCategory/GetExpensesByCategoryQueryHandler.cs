using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Dashboard.Queries.GetExpensesByCategory;

public class GetExpensesByCategoryQueryHandler(
    ITransactionRepository transactionRepository)
    : IRequestHandler<GetExpensesByCategoryQuery, IEnumerable<ExpensesByCategoryDto>>
{
    public async Task<IEnumerable<ExpensesByCategoryDto>> Handle(
        GetExpensesByCategoryQuery request,
        CancellationToken cancellationToken)
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
            type: TransactionType.Expense,
            status: null,
            amountMin: null,
            amountMax: null,
            search: null,
            cancellationToken: cancellationToken);

        var expenses = transactions
            .Where(t => t.Status != TransactionStatus.Scheduled)
            .ToList();

        var totalExpenses = expenses.Sum(t => t.Amount);

        if (totalExpenses == 0)
            return [];

        return expenses
            .GroupBy(t => t.CategoryId)
            .Select(g =>
            {
                var first = g.First();
                var total = g.Sum(t => t.Amount);
                var percentage = Math.Round(total / totalExpenses * 100, 2);

                return new ExpensesByCategoryDto(
                    CategoryId: first.CategoryId.ToString(),
                    CategoryName: first.Category.Name,
                    CategoryIcon: first.Category.Icon,
                    CategoryColor: first.Category.Color,
                    Total: total,
                    Percentage: percentage);
            })
            .OrderByDescending(x => x.Total);
    }
}
