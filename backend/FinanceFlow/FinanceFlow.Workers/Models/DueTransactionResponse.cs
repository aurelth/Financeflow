namespace FinanceFlow.Workers.Models;

public record DueTransactionResponse(
    Guid Id,
    Guid UserId,
    string Description,
    decimal Amount,
    DateTime Date,
    bool IsRecurring,
    string RecurrenceType,
    string Type,
    bool NotifyDueTomorrow = true,
    bool NotifyDueIn3Days = true
);
