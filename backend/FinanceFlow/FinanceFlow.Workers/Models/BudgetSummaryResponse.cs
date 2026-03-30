namespace FinanceFlow.Workers.Models;

public record BudgetSummaryResponse(
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
