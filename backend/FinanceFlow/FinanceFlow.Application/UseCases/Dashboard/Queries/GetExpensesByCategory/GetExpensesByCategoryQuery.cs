using FinanceFlow.Application.DTOs;
using MediatR;

namespace FinanceFlow.Application.UseCases.Dashboard.Queries.GetExpensesByCategory;

public record GetExpensesByCategoryQuery(
    Guid UserId,
    int Month,
    int Year
) : IRequest<IEnumerable<ExpensesByCategoryDto>>;
