using AutoMapper;
using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Budgets.Commands.UpdateBudget;

public class UpdateBudgetCommandHandler(
    IBudgetRepository budgetRepository,
    IMapper mapper)
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

        return mapper.Map<BudgetDto>(updated);
    }
}
