using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Application.DTOs.Goals;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Goals.Commands.UpdateGoal;

public class UpdateGoalCommandHandler(
    IGoalRepository goalRepository,
    ICategoryRepository categoryRepository,
    IGoalProgressService goalProgressService) : IRequestHandler<UpdateGoalCommand, GoalProgressResultDto>
{
    public async Task<GoalProgressResultDto> Handle(
        UpdateGoalCommand request,
        CancellationToken cancellationToken)
    {
        var goal = await goalRepository.GetByIdAsync(request.Id, request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Goal), request.Id);

        var nameChanged = goal.Name != request.Name;
        var emojiChanged = goal.Emoji != request.Emoji;

        goal.Name = request.Name;
        goal.TargetAmount = request.TargetAmount;
        goal.MonthlyContribution = request.MonthlyContribution;
        goal.Deadline = request.Deadline;
        goal.Emoji = request.Emoji;

        if ((nameChanged || emojiChanged) && goal.LinkedCategoryId.HasValue)
        {
            var category = await categoryRepository.GetByIdForUpdateAsync(
                goal.LinkedCategoryId.Value, request.UserId, cancellationToken);

            if (category is not null)
            {
                category.Name = $"Meta: {request.Name}";
                category.Icon = request.Emoji;
                await categoryRepository.UpdateAsync(category, cancellationToken);
            }
        }

        await goalRepository.UpdateAsync(goal, cancellationToken);

        var summary = await goalProgressService.CalculateAsync(request.UserId, cancellationToken);
        var result = summary.Goals.FirstOrDefault(g => g.Id == request.Id)
            ?? throw new NotFoundException(nameof(Goal), request.Id);

        return result;
    }
}
