namespace FinanceFlow.Application.Events;

public record BankImportCreatedEvent(
    Guid ImportId,
    Guid UserId,
    string FileName,
    DateTime CreatedAt
);
