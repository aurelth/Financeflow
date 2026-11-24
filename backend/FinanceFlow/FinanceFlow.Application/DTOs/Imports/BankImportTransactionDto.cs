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
    DuplicateMatchDto? Match = null,
    TransferMatchDto? TransferMatch = null
);

/// <summary>Lançamento já existente com o qual a linha do extrato foi confundida.</summary>
public record DuplicateMatchDto(
    Guid TransactionId,
    DateTime Date,
    decimal Amount,
    string Description
);

/// <summary>Lançamento de outra conta que parece ser a outra ponta da transferência.</summary>
/// <param name="Status">"Auto" (vinculado sozinho) ou "Suggested" (o usuário decide).</param>
/// <param name="WillConvert">Verdadeiro quando o lançamento hoje é receita/despesa e vira transferência ao vincular.</param>
public record TransferMatchDto(
    Guid TransactionId,
    Guid? AccountId,
    string? AccountName,
    DateTime Date,
    decimal Amount,
    string Description,
    int Score,
    string Status,
    bool WillConvert
);
