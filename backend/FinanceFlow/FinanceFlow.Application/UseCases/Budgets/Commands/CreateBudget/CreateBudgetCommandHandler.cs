using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using MediatR;
using FinanceFlow.Application.Common.Mappings;

namespace FinanceFlow.Application.UseCases.Budgets.Commands.CreateBudget;

public class CreateBudgetCommandHandler(
    IBudgetRepository budgetRepository,
    ICategoryRepository categoryRepository)
    : IRequestHandler<CreateBudgetCommand, BudgetDto>
{
    public async Task<BudgetDto> Handle(
        CreateBudgetCommand request,
        CancellationToken cancellationToken)
    {
        var category = await categoryRepository.GetByIdAsync(
            request.CategoryId, request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Category), request.CategoryId);

        var alreadyExists = await budgetRepository.ExistsAsync(
            request.UserId, request.CategoryId, request.Month, request.Year, cancellationToken);

        if (alreadyExists)
            throw new ValidationException(
                $"Já existe um orçamento para a categoria '{category.Name}' em {request.Month}/{request.Year}.");

        var budget = new Budget
        {
            UserId = request.UserId,
            CategoryId = request.CategoryId,
            Month = request.Month,
            Year = request.Year,
            LimitAmount = request.LimitAmount
        };

        await budgetRepository.AddAsync(budget, cancellationToken);

        var created = await budgetRepository.GetByIdAsync(
            budget.Id, request.UserId, cancellationToken);

        // O registro acabou de ser gravado: se não volta na leitura, algo está errado (ex.: exclusão concorrente)
        if (created is null)
            throw new InvalidOperationException(
                "Orçamento criado, mas não foi possível recarregá-lo para montar a resposta.");

        return created.ToDto();
    }
}
