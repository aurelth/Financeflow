using AutoMapper;
using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Budgets.Commands.CreateBudget;

public class CreateBudgetCommandHandler(
    IBudgetRepository budgetRepository,
    ICategoryRepository categoryRepository,
    IMapper mapper)
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

        return mapper.Map<BudgetDto>(created);
    }
}
