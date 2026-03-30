namespace FinanceFlow.Application.DTOs;

public record PeriodDataDto(
    int Month,
    int Year,
    decimal TotalIncome,
    decimal TotalExpenses,
    decimal Balance
);
