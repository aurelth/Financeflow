using FinanceFlow.Application.DTOs.CategoryRules;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.CategoryRules.Queries.GetCategoryRules;

public class GetCategoryRulesQueryHandler(ICategoryRuleRepository categoryRuleRepository)
    : IRequestHandler<GetCategoryRulesQuery, IReadOnlyList<CategoryRuleDto>>
{
    public async Task<IReadOnlyList<CategoryRuleDto>> Handle(
        GetCategoryRulesQuery request,
        CancellationToken cancellationToken)
    {
        var rules = await categoryRuleRepository.GetAllByUserAsync(request.UserId, cancellationToken);

        return rules
            .Select(r => new CategoryRuleDto(
                Id: r.Id,
                Key: r.Key,
                Type: r.Type,
                CategoryId: r.CategoryId,
                CategoryName: r.Category.Name,
                CategoryIcon: r.Category.Icon,
                CategoryColor: r.Category.Color,
                HitCount: r.HitCount,
                LastUsedAt: r.LastUsedAt))
            .ToList();
    }
}
