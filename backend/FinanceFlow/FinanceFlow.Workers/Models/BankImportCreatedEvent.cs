namespace FinanceFlow.Workers.Models;

public record BankImportCreatedEvent(
    Guid ImportId,
    Guid UserId,
    string FileName,
    DateTime CreatedAt
);
