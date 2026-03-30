using MediatR;

namespace FinanceFlow.Application.UseCases.Budgets.Commands.DeleteBudget;

public record DeleteBudgetCommand(
    Guid Id,
    Guid UserId
) : IRequest;
