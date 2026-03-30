using FinanceFlow.Application.DTOs;
using MediatR;

namespace FinanceFlow.Application.UseCases.Budgets.Commands.UpdateBudget;

public record UpdateBudgetCommand(
    Guid Id,
    Guid UserId,
    decimal LimitAmount
) : IRequest<BudgetDto>;
