namespace FinanceFlow.Application.DTOs.HealthScore;

public record HealthScoreResultDto(
    int Score,
    string Classification,
    IEnumerable<ScoreDetailDto> Details
);

public record ScoreDetailDto(
    string Criterion,
    int Points,
    int MaxPoints,
    string Justification
);
