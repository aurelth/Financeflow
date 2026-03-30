namespace FinanceFlow.Application.DTOs;

public record WeeklyComparisonDto(
    int Week,
    string Label,
    decimal Income,
    decimal Expenses
);
