namespace FinanceFlow.Application.DTOs.HealthScore;

public record HealthScoreHistoryItem(
    int Month,
    int Year,
    string MonthLabel,
    int Score,
    string Classification
);
