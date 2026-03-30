using FinanceFlow.Application.DTOs;
using MediatR;

namespace FinanceFlow.Application.UseCases.Budgets.Queries.GetBudgetSummary;

public record GetBudgetSummaryQuery(
    Guid UserId,
    int Month,
    int Year
) : IRequest<IEnumerable<BudgetSummaryDto>>;
