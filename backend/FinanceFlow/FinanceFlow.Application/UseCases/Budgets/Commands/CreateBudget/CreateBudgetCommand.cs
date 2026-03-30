using FinanceFlow.Application.DTOs;
using MediatR;

namespace FinanceFlow.Application.UseCases.Budgets.Commands.CreateBudget;

public record CreateBudgetCommand(
    Guid UserId,
    Guid CategoryId,
    int Month,
    int Year,
    decimal LimitAmount
) : IRequest<BudgetDto>;
