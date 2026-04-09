using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.DTOs;
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
        var prefs = await repository.GetByUserIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("Preferências não encontradas.");

        return new NotificationPreferencesDto(
            BudgetWarningEnabled: prefs.BudgetWarningEnabled,
            BudgetCriticalEnabled: prefs.BudgetCriticalEnabled,
            TransactionDueTomorrowEnabled: prefs.TransactionDueTomorrowEnabled,
            TransactionDueIn3DaysEnabled: prefs.TransactionDueIn3DaysEnabled
        );
    }
}
