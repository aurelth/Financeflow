namespace FinanceFlow.Application.DTOs;

public record PeriodComparisonDto(
    IEnumerable<PeriodDataDto> Periods,
    IEnumerable<CategoryComparisonDto> CategoryComparisons
);
