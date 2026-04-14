namespace FinanceFlow.Application.DTOs.Reports;

public record AnnualSummaryDto(
    int Year,
    IEnumerable<AnnualMonthDto> Months,
    decimal TotalIncome,
    decimal TotalExpenses,
    decimal NetBalance,
    decimal AverageMonthlyIncome,
    decimal AverageMonthlyExpenses
);

public record AnnualMonthDto(
    int Month,
    string MonthName,
    decimal Income,
    decimal Expenses,
    decimal Balance,
    decimal CumulativeBalance
);
