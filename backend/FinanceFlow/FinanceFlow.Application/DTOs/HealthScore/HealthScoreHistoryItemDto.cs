namespace FinanceFlow.Application.DTOs.HealthScore;

public record HealthScoreHistoryItemDto(
    int Month,
    int Year,
    string MonthLabel,
    int Score,
    string Classification
);
