using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;

namespace FinanceFlow.Application.DTOs.Imports;

public record BankImportTransactionDto(
    Guid Id,
    string ExternalId,
    DateTime Date,
    decimal Amount,
    string Description,
    TransactionType Type,
    bool IsDuplicate,
    bool IsSelected,
    Guid? SuggestedCategoryId,
    string? SuggestedCategoryName,
    DuplicateReason? DuplicateReason = null,
    DuplicateMatchDto? Match = null
);

/// <summary>Lançamento já existente com o qual a linha do extrato foi confundida.</summary>
public record DuplicateMatchDto(
    Guid TransactionId,
    DateTime Date,
    decimal Amount,
    string Description
);
