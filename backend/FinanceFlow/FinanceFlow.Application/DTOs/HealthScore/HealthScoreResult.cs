namespace FinanceFlow.Application.DTOs.HealthScore;

public record HealthScoreResult(
    int Score,
    string Classification,
    IEnumerable<ScoreDetail> Details
);

public record ScoreDetail(
    string Criterion,
    int Points,
    int MaxPoints,
    string Justification
);
