namespace FinanceFlow.Application.DTOs.Goals;

public record GoalProgressResult(
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
    string Status  // "OnTrack", "Behind", "Completed", "Overdue"
);

public record GoalsSummaryResult(
    decimal AvailableThisMonth,
    decimal CommittedThisMonth,
    decimal Difference,
    IEnumerable<GoalProgressResult> Goals
);
