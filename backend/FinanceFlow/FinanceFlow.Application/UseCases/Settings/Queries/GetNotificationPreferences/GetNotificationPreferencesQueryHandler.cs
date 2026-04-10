using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Settings.Queries.GetNotificationPreferences;

public class GetNotificationPreferencesQueryHandler(
    INotificationPreferencesRepository repository
) : IRequestHandler<GetNotificationPreferencesQuery, NotificationPreferencesDto>
{
    public async Task<NotificationPreferencesDto> Handle(
        GetNotificationPreferencesQuery request,
        CancellationToken cancellationToken)
    {
        var prefs = await repository.GetByUserIdAsync(request.UserId, cancellationToken);

        // Cria preferências padrão para utilizadores antigos
        if (prefs is null)
        {
            prefs = new UserNotificationPreferences
            {
                UserId = request.UserId,
                BudgetWarningEnabled = true,
                BudgetCriticalEnabled = true,
                TransactionDueTomorrowEnabled = true,
                TransactionDueIn3DaysEnabled = true,
            };
            await repository.CreateAsync(prefs, cancellationToken);
        }

        return new NotificationPreferencesDto(
            BudgetWarningEnabled: prefs.BudgetWarningEnabled,
            BudgetCriticalEnabled: prefs.BudgetCriticalEnabled,
            TransactionDueTomorrowEnabled: prefs.TransactionDueTomorrowEnabled,
            TransactionDueIn3DaysEnabled: prefs.TransactionDueIn3DaysEnabled
        );
    }
}
