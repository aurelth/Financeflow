namespace FinanceFlow.Application.DTOs;

public record BudgetSummaryDto(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string CategoryIcon,
    string CategoryColor,
    int Month,
    int Year,
    decimal LimitAmount,
    decimal SpentAmount,
    decimal Percentage
);
