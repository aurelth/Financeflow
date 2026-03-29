using FinanceFlow.Domain.Entities;

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
    DateTime? UpdatedAt
);
