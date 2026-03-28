using FinanceFlow.Domain.Entities;

namespace FinanceFlow.Application.DTOs;

public record CreateTransactionRequestDto(
    decimal Amount,
    TransactionType Type,
    DateTime Date,
    string Description,
    TransactionStatus Status,
    bool IsRecurring,
    RecurrenceType RecurrenceType,
    Guid CategoryId,
    Guid? SubcategoryId,
    string[] Tags
);
