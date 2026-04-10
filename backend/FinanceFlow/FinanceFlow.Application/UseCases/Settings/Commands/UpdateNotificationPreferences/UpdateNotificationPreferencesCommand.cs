using MediatR;

namespace FinanceFlow.Application.UseCases.Settings.Commands.UpdateNotificationPreferences;

public record UpdateNotificationPreferencesCommand(
    Guid UserId,
    bool BudgetWarningEnabled,
    bool BudgetCriticalEnabled,
    bool TransactionDueTomorrowEnabled,
    bool TransactionDueIn3DaysEnabled
) : IRequest;
