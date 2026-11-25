using FinanceFlow.Application.DTOs.WeeklySummary;
using FinanceFlow.Application.Services;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.WeeklySummary.Queries.GetWeeklySummaryCandidates;

public class GetWeeklySummaryCandidatesQueryHandler(
    IUserRepository userRepository,
    TimeProvider clock)
    : IRequestHandler<GetWeeklySummaryCandidatesQuery, IReadOnlyList<WeeklySummaryCandidateDto>>
{
    public async Task<IReadOnlyList<WeeklySummaryCandidateDto>> Handle(
        GetWeeklySummaryCandidatesQuery request,
        CancellationToken cancellationToken)
    {
        var users = await userRepository.GetActiveWithNotificationPreferencesAsync(cancellationToken);
        var utcNow = clock.GetUtcNow();

        var candidates = new List<WeeklySummaryCandidateDto>();

        foreach (var user in users)
        {
            // Cada usuário tem a sua segunda-feira: às 8h em São Paulo ainda é madrugada em Lisboa e vice-versa
            var localNow = UserClock.LocalNow(user.Timezone, utcNow);
            if (!WeeklySummaryCalendar.IsDeliveryWindow(localNow))
                continue;

            // Sem preferências gravadas (usuário antigo) vale o padrão: tudo ligado
            var inApp = user.NotificationPreferences?.WeeklySummaryEnabled ?? true;
            var email = user.NotificationPreferences?.WeeklySummaryEmailEnabled ?? true;
            if (!inApp && !email)
                continue;

            candidates.Add(new WeeklySummaryCandidateDto(
                UserId: user.Id,
                WeekStart: WeeklySummaryCalendar.PreviousWeekStart(localNow.Date),
                NotifyInApp: inApp,
                NotifyEmail: email));
        }

        return candidates;
    }
}
