using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Budgets.Commands.DeleteBudget;

public class DeleteBudgetCommandHandler(
    IBudgetRepository budgetRepository)
    : IRequestHandler<DeleteBudgetCommand>
{
    public async Task Handle(
        DeleteBudgetCommand request,
        CancellationToken cancellationToken)
    {
        var budget = await budgetRepository.GetByIdAsync(
            request.Id, request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Budget), request.Id);

        await budgetRepository.DeleteAsync(budget, cancellationToken);
    }
}
