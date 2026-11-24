using MediatR;

namespace FinanceFlow.Application.UseCases.CategoryRules.Commands.DeleteCategoryRule;

public record DeleteCategoryRuleCommand(Guid UserId, Guid RuleId) : IRequest;
