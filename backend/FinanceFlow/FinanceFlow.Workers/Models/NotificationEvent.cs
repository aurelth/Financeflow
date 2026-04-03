namespace FinanceFlow.Workers.Models;

public record NotificationEvent(
    Guid UserId,
    string Message,
    string Type, // "BudgetWarning" | "BudgetCritical"
    DateTime CreatedAt,
    Guid? ReferenceId = null
);
