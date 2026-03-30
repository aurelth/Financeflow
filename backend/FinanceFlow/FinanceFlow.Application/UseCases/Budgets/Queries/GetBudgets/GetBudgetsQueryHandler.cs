using AutoMapper;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Budgets.Queries.GetBudgets;

public class GetBudgetsQueryHandler(
    IBudgetRepository budgetRepository,
    IMapper mapper)
    : IRequestHandler<GetBudgetsQuery, IEnumerable<BudgetDto>>
{
    public async Task<IEnumerable<BudgetDto>> Handle(
        GetBudgetsQuery request,
        CancellationToken cancellationToken)
    {
        var budgets = await budgetRepository.GetByUserAndPeriodAsync(
            request.UserId, request.Month, request.Year, cancellationToken);

        return mapper.Map<IEnumerable<BudgetDto>>(budgets);
    }
}
