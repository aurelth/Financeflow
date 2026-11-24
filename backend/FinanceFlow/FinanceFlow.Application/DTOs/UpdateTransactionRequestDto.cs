using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;

namespace FinanceFlow.Application.DTOs;

public record UpdateTransactionRequestDto(
    decimal Amount,
    TransactionType Type,
    DateTime Date,
    string Description,
    TransactionStatus Status,
    bool IsRecurring,
    RecurrenceType RecurrenceType,
    Guid CategoryId,
    Guid? SubcategoryId,
    string[] Tags,
    bool PropagateToFuture = false,
    Guid? AccountId = null,
    TransferDirection? TransferDirection = null
);
