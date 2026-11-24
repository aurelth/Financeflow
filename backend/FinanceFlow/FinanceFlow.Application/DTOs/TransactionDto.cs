using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;

namespace FinanceFlow.Application.DTOs;

public record TransactionDto(
    Guid Id,
    decimal Amount,
    TransactionType Type,
    DateTime Date,
    string Description,
    TransactionStatus Status,
    bool IsRecurring,
    RecurrenceType RecurrenceType,
    Guid? RecurrenceGroupId,
    string? AttachmentPath,
    string? AttachmentName,
    string[] Tags,
    Guid CategoryId,
    string CategoryName,
    string CategoryIcon,
    string CategoryColor,
    Guid? SubcategoryId,
    string? SubcategoryName,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    Guid AccountId = default,
    string? AccountName = null,
    // Transferência entre contas: sentido da ponta e, se já pareada, a outra ponta
    TransferDirection? TransferDirection = null,
    Guid? TransferLinkId = null,
    Guid? CounterpartTransactionId = null,
    Guid? CounterpartAccountId = null,
    string? CounterpartAccountName = null
);
