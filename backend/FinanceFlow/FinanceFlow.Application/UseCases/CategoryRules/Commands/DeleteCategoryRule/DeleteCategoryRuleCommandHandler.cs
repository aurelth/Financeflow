using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.CategoryRules.Commands.DeleteCategoryRule;

public class DeleteCategoryRuleCommandHandler(ICategoryRuleRepository categoryRuleRepository)
    : IRequestHandler<DeleteCategoryRuleCommand>
{
    public async Task Handle(DeleteCategoryRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = await categoryRuleRepository.GetByIdAsync(request.RuleId, request.UserId, cancellationToken)
            ?? throw new NotFoundException("CategoryRule", request.RuleId);

        categoryRuleRepository.Remove(rule);
        await categoryRuleRepository.SaveChangesAsync(cancellationToken);
    }
}
