namespace FinanceFlow.Application.DTOs.Reports;

public record ProjectionsDto(
    int MonthsAnalysed,
    int MonthsAhead,
    IEnumerable<ProjectionMonthDto> Historical,
    IEnumerable<ProjectionMonthDto> Projected
);

public record ProjectionMonthDto(
    int Year,
    int Month,
    string MonthName,
    decimal Income,
    decimal Expenses,
    decimal Balance,
    bool IsProjected
);
