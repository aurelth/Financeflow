namespace FinanceFlow.Workers.Models;

public record DueTransactionResponse(
    Guid Id,
    Guid UserId,
    string Description,
    decimal Amount,
    DateTime Date,
    bool IsRecurring,
    string RecurrenceType
);
