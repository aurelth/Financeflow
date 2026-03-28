using FinanceFlow.Domain.Entities;

namespace FinanceFlow.Application.Events;

public record TransactionCreatedEvent(
    Guid TransactionId,
    Guid UserId,
    decimal Amount,
    TransactionType Type,
    DateTime Date,
    string Description,
    TransactionStatus Status,
    Guid CategoryId,
    string CategoryName,
    DateTime CreatedAt
);
