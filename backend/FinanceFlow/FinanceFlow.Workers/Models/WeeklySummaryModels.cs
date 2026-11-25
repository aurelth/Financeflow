namespace FinanceFlow.Workers.Models;

public record WeeklySummaryCandidateResponse(
    Guid UserId,
    DateTime WeekStart,
    bool NotifyInApp,
    bool NotifyEmail);

public record WeeklySummaryNotificationResponse(
    Guid UserId,
    DateTime WeekStart,
    Guid ReferenceId,
    string Message);
