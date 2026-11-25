using FinanceFlow.Application.DTOs.WeeklySummary;
using FinanceFlow.Application.Services;
using FinanceFlow.Application.UseCases.WeeklySummary.Queries.GetWeeklySummary;
using MediatR;

namespace FinanceFlow.Application.UseCases.WeeklySummary.Queries.GetWeeklySummaryNotification;

public class GetWeeklySummaryNotificationQueryHandler(ISender sender)
    : IRequestHandler<GetWeeklySummaryNotificationQuery, WeeklySummaryNotificationDto?>
{
    public async Task<WeeklySummaryNotificationDto?> Handle(
        GetWeeklySummaryNotificationQuery request,
        CancellationToken cancellationToken)
    {
        var summary = await sender.Send(new GetWeeklySummaryQuery(request.UserId), cancellationToken);

        // Semana sem movimento, sem nada a vencer e sem alerta: não vale incomodar o usuário
        if (!summary.HasActivity)
            return null;

        return new WeeklySummaryNotificationDto(
            UserId: request.UserId,
            WeekStart: summary.WeekStart,
            ReferenceId: WeeklySummaryCalendar.ReferenceId(request.UserId, summary.WeekStart),
            Message: WeeklySummaryText.BuildNotificationMessage(summary));
    }
}
