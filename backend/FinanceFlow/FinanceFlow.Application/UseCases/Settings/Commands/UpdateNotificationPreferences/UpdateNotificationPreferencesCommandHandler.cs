using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Settings.Commands.UpdateNotificationPreferences;

public class UpdateNotificationPreferencesCommandHandler(
    INotificationPreferencesRepository repository
) : IRequestHandler<UpdateNotificationPreferencesCommand>
{
    public async Task Handle(
        UpdateNotificationPreferencesCommand request,
        CancellationToken cancellationToken)
    {
        var prefs = await repository.GetByUserIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("Preferências não encontradas.");

        prefs.BudgetWarningEnabled = request.BudgetWarningEnabled;
        prefs.BudgetCriticalEnabled = request.BudgetCriticalEnabled;
        prefs.TransactionDueTomorrowEnabled = request.TransactionDueTomorrowEnabled;
        prefs.TransactionDueIn3DaysEnabled = request.TransactionDueIn3DaysEnabled;
        prefs.TransactionDueTodayEnabled = request.TransactionDueTodayEnabled;
        prefs.TransactionOverdueEnabled = request.TransactionOverdueEnabled;
        prefs.WeeklySummaryEnabled = request.WeeklySummaryEnabled;
        prefs.WeeklySummaryEmailEnabled = request.WeeklySummaryEmailEnabled;

        await repository.UpdateAsync(prefs, cancellationToken);
    }
}
