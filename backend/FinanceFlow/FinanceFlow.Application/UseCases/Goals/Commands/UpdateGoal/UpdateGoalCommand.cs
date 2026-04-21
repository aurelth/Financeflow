using FinanceFlow.Application.DTOs.Goals;
using MediatR;

namespace FinanceFlow.Application.UseCases.Goals.Commands.UpdateGoal;

public record UpdateGoalCommand(
    Guid Id,
    Guid UserId,
    string Name,
    decimal TargetAmount,
    decimal MonthlyContribution,
    DateTime Deadline,
    string Emoji
) : IRequest<GoalProgressResultDto>;
