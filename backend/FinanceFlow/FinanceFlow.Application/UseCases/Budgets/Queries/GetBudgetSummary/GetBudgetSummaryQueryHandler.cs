using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Budgets.Queries.GetBudgetSummary;

public class GetBudgetSummaryQueryHandler(
    IBudgetRepository budgetRepository,
    ITransactionRepository transactionRepository)
    : IRequestHandler<GetBudgetSummaryQuery, IEnumerable<BudgetSummaryDto>>
{
    public async Task<IEnumerable<BudgetSummaryDto>> Handle(
        GetBudgetSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var budgets = await budgetRepository.GetByUserAndPeriodAsync(
            request.UserId, request.Month, request.Year, cancellationToken);

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

        var spentByCategory = transactions
            .GroupBy(t => t.CategoryId)
            .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));

        return budgets.Select(b =>
        {
            var spent = spentByCategory.GetValueOrDefault(b.CategoryId, 0);
            var percentage = b.LimitAmount > 0
                ? Math.Round(spent / b.LimitAmount * 100, 2)
                : 0;

            return new BudgetSummaryDto(
                Id: b.Id,
                CategoryId: b.CategoryId,
                CategoryName: b.Category.Name,
                CategoryIcon: b.Category.Icon,
                CategoryColor: b.Category.Color,
                Month: b.Month,
                Year: b.Year,
                LimitAmount: b.LimitAmount,
                SpentAmount: spent,
                Percentage: percentage
            );
        });
    }
}
