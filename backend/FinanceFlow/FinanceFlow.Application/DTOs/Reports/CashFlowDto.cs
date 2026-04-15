namespace FinanceFlow.Application.DTOs.Reports;

public record CashFlowDto(
    DateTime From,
    DateTime To,
    string GroupBy,
    IEnumerable<CashFlowPeriodDto> Periods,
    decimal TotalIncome,
    decimal TotalExpenses,
    decimal NetBalance
);

public record CashFlowPeriodDto(
    string Label,
    decimal Income,
    decimal Expenses,
    decimal Balance,
    decimal CumulativeBalance
);
