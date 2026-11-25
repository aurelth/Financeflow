namespace FinanceFlow.Application.DTOs;

public record NotificationPreferencesDto(
    bool BudgetWarningEnabled,
    bool BudgetCriticalEnabled,
    bool TransactionDueTomorrowEnabled,
    bool TransactionDueIn3DaysEnabled,
    bool TransactionDueTodayEnabled = true,
    bool TransactionOverdueEnabled = true,
    bool WeeklySummaryEnabled = true,
    bool WeeklySummaryEmailEnabled = true
);
