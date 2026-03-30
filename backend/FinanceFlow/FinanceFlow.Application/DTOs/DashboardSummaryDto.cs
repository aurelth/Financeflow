namespace FinanceFlow.Application.DTOs;

public record DashboardSummaryDto(
    decimal TotalIncome,
    decimal TotalExpenses,
    decimal Balance,
    decimal ProjectedBalance,
    int Month,
    int Year
);
