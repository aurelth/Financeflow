using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;

namespace FinanceFlow.Application.DTOs.Transfers;

/// <summary>Uma ponta de transferência (um lançamento numa das contas).</summary>
public record TransferSideDto(
    Guid TransactionId,
    Guid AccountId,
    string? AccountName,
    DateTime Date,
    decimal Amount,
    string Description,
    TransactionType Type,
    TransferDirection? Direction)
{
    public static TransferSideDto From(Transaction t, string? accountName = null) => new(
        TransactionId: t.Id,
        AccountId: t.AccountId,
        AccountName: accountName ?? t.Account?.Name,
        Date: t.Date,
        Amount: t.Amount,
        Description: t.Description,
        Type: t.Type,
        Direction: t.TransferDirection);
}

/// <summary>Transferência pareada: saída numa conta, entrada na outra.</summary>
public record TransferLinkDto(
    Guid Id,
    TransferSideDto Out,
    TransferSideDto In,
    string Origin,
    int? Score,
    DateTime CreatedAt);

public record CreateTransferRequestDto(
    Guid FromAccountId,
    Guid ToAccountId,
    decimal Amount,
    DateTime Date,
    string? Description = null);

/// <summary>Une dois lançamentos existentes (um que saiu de uma conta e outro que entrou em outra).</summary>
public record LinkTransfersRequestDto(
    Guid FirstTransactionId,
    Guid SecondTransactionId);

/// <summary>
/// Como desfazer o vínculo. Ou as duas pontas voltam a ser despesa/receita (cada uma com a sua categoria),
/// ou os dois lançamentos são excluídos.
/// </summary>
public record UnlinkTransferRequestDto(
    Guid? OutCategoryId = null,
    Guid? InCategoryId = null,
    bool DeleteTransactions = false);

/// <summary>Sugestão de par para uma ponta que ainda não tem outra metade.</summary>
public record TransferSuggestionDto(
    TransferSideDto Candidate,
    int Score,
    bool WillConvert);

/// <summary>Ponta de transferência sem par, com a melhor sugestão (se houver).</summary>
public record PendingTransferDto(
    TransferSideDto Transaction,
    TransferSuggestionDto? Suggestion);
