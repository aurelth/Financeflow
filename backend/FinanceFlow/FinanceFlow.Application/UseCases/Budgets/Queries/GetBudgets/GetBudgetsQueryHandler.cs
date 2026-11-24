using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Interfaces;
using MediatR;
using FinanceFlow.Application.Common.Mappings;

namespace FinanceFlow.Application.UseCases.Budgets.Queries.GetBudgets;

public class GetBudgetsQueryHandler(
    IBudgetRepository budgetRepository)
    : IRequestHandler<GetBudgetsQuery, IEnumerable<BudgetDto>>
{
    public async Task<IEnumerable<BudgetDto>> Handle(
        GetBudgetsQuery request,
        CancellationToken cancellationToken)
    {
        var budgets = await budgetRepository.GetByUserAndPeriodAsync(
            request.UserId, request.Month, request.Year, cancellationToken);

        return budgets.ToDtos();
    }
}
