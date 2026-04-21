using FinanceFlow.Application.DTOs.Goals;
using MediatR;

namespace FinanceFlow.Application.UseCases.Goals.Commands.CreateGoal;

public record CreateGoalCommand(
    Guid UserId,
    string Name,
    decimal TargetAmount,
    decimal MonthlyContribution,
    DateTime Deadline,
    string Emoji
) : IRequest<GoalProgressResultDto>;
