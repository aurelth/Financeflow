namespace FinanceFlow.Application.Events;

public record ReportRequestedEvent(
    Guid ReportId,
    Guid UserId,
    int Month,
    int Year,
    DateTime RequestedAt
);
