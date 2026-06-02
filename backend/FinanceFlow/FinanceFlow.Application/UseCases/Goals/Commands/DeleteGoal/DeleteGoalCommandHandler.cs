using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Goals.Commands.DeleteGoal;

public class DeleteGoalCommandHandler(
    IGoalRepository goalRepository,
    ICategoryRepository categoryRepository) : IRequestHandler<DeleteGoalCommand>
{
    public async Task Handle(
        DeleteGoalCommand request,
        CancellationToken cancellationToken)
    {
        var goal = await goalRepository.GetByIdAsync(request.Id, request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Goal), request.Id);

        if (goal.LinkedCategoryId.HasValue)
        {
            var category = await categoryRepository.GetByIdForUpdateAsync(
                goal.LinkedCategoryId.Value, request.UserId, cancellationToken);

            if (category is not null)
            {
                category.IsArchived = true;
                await categoryRepository.UpdateAsync(category, cancellationToken);
            }
        }

        await goalRepository.DeleteAsync(goal, cancellationToken);
    }
}
