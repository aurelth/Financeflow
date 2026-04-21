namespace FinanceFlow.Application.DTOs.Goals;

public record CreateGoalRequestDto(
    string Name,
    decimal TargetAmount,
    decimal MonthlyContribution,
    DateTime Deadline,
    string Emoji
);

public record UpdateGoalRequestDto(
    string Name,
    decimal TargetAmount,
    decimal MonthlyContribution,
    DateTime Deadline,
    string Emoji
);
