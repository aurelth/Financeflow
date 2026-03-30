namespace FinanceFlow.Workers.Models;

public record BudgetAlertEvent(
    Guid UserId,
    Guid CategoryId,
    string CategoryName,
    int Month,
    int Year,
    decimal LimitAmount,
    decimal SpentAmount,
    decimal Percentage,
    string AlertLevel, // "warning" (80%) ou "critical" (100%)
    DateTime TriggeredAt
);
