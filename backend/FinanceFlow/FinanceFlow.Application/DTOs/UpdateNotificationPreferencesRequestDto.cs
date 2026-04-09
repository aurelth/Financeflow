namespace FinanceFlow.Application.DTOs;

public record UpdateNotificationPreferencesRequestDto(
    bool BudgetWarningEnabled,
    bool BudgetCriticalEnabled,
    bool TransactionDueTomorrowEnabled,
    bool TransactionDueIn3DaysEnabled
);
