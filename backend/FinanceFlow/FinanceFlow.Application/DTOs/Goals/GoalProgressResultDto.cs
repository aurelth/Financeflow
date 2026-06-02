namespace FinanceFlow.Application.DTOs.Goals;

public record GoalProgressResultDto(
    Guid Id,
    string Name,
    string Emoji,
    decimal TargetAmount,
    decimal MonthlyContribution,
    DateTime Deadline,
    decimal AccumulatedAmount,
    decimal PlannedThisMonth,
    decimal ReceivedThisMonth,
    decimal ProgressPercentage,
    bool IsCompleted,
    int? MonthsToComplete,
    string Status,  // "OnTrack", "Behind", "Completed", "Overdue"
    Guid? LinkedCategoryId
);

public record GoalsSummaryResultDto(
    decimal AvailableThisMonth,
    decimal CommittedThisMonth,
    decimal Difference,
    IEnumerable<GoalProgressResultDto> Goals
);
