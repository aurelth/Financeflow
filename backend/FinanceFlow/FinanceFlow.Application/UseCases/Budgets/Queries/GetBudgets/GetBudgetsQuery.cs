using FinanceFlow.Application.DTOs;
using MediatR;

namespace FinanceFlow.Application.UseCases.Budgets.Queries.GetBudgets;

public record GetBudgetsQuery(
    Guid UserId,
    int Month,
    int Year
) : IRequest<IEnumerable<BudgetDto>>;
