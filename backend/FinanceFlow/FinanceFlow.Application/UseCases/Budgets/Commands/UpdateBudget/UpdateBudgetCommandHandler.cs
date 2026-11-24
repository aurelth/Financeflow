using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using MediatR;
using FinanceFlow.Application.Common.Mappings;

namespace FinanceFlow.Application.UseCases.Budgets.Commands.UpdateBudget;

public class UpdateBudgetCommandHandler(
    IBudgetRepository budgetRepository)
    : IRequestHandler<UpdateBudgetCommand, BudgetDto>
{
    public async Task<BudgetDto> Handle(
        UpdateBudgetCommand request,
        CancellationToken cancellationToken)
    {
        var budget = await budgetRepository.GetByIdAsync(
            request.Id, request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Budget), request.Id);

        budget.LimitAmount = request.LimitAmount;
        budget.UpdatedAt = DateTime.UtcNow;

        await budgetRepository.UpdateAsync(budget, cancellationToken);

        var updated = await budgetRepository.GetByIdAsync(
            budget.Id, request.UserId, cancellationToken);

        // O registro acabou de ser gravado: se não volta na leitura, algo está errado (ex.: exclusão concorrente)
        if (updated is null)
            throw new InvalidOperationException(
                "Orçamento atualizado, mas não foi possível recarregá-lo para montar a resposta.");

        return updated.ToDto();
    }
}
