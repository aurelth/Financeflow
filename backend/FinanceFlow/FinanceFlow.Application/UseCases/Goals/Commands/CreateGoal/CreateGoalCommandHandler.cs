using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Application.DTOs.Goals;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Goals.Commands.CreateGoal;

public class CreateGoalCommandHandler(
    IGoalRepository goalRepository,
    ICategoryRepository categoryRepository,
    IGoalProgressService goalProgressService) : IRequestHandler<CreateGoalCommand, GoalProgressResultDto>
{
    public async Task<GoalProgressResultDto> Handle(
        CreateGoalCommand request,
        CancellationToken cancellationToken)
    {        
        var category = new Category
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Name = $"Meta: {request.Name}",
            Icon = request.Emoji,
            Color = "#6366f1",
            Type = TransactionType.Expense,
            IsDefault = false,
            IsActive = true,
            IsGoalCategory = true,
            IsArchived = false,
            CreatedAt = DateTime.UtcNow,
        };

        await categoryRepository.AddAsync(category, cancellationToken);

        var goal = new Goal
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Name = request.Name,
            TargetAmount = request.TargetAmount,
            MonthlyContribution = request.MonthlyContribution,
            Deadline = request.Deadline,
            Emoji = request.Emoji,
            LinkedCategoryId = category.Id,
            CreatedAt = DateTime.UtcNow,
        };

        await goalRepository.AddAsync(goal, cancellationToken);
        
        var summary = await goalProgressService.CalculateAsync(request.UserId, cancellationToken);
        var result = summary.Goals.FirstOrDefault(g => g.Id == goal.Id)
            ?? throw new NotFoundException(nameof(Goal), goal.Id);

        return result;
    }
}
