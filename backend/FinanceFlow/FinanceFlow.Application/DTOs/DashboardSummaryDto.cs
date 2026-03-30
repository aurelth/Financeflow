namespace FinanceFlow.Application.DTOs;

public record DashboardSummaryDto(
    decimal TotalIncome,
    decimal TotalExpenses,
    decimal Balance,
    int Month,
    int Year
);
