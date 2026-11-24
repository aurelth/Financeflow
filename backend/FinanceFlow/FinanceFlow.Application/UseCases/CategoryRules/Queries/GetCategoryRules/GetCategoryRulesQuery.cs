using FinanceFlow.Application.DTOs.CategoryRules;
using MediatR;

namespace FinanceFlow.Application.UseCases.CategoryRules.Queries.GetCategoryRules;

public record GetCategoryRulesQuery(Guid UserId) : IRequest<IReadOnlyList<CategoryRuleDto>>;
