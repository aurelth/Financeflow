namespace FinanceFlow.Application.DTOs;

public record DueTransactionDto(
    Guid Id,
    Guid UserId,
    string Description,
    decimal Amount,
    DateTime Date,
    bool IsRecurring,
    string RecurrenceType,
    string Type,
    bool NotifyDueTomorrow,
    bool NotifyDueIn3Days
);
