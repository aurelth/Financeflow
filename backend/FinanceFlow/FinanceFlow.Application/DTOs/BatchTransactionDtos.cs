using FinanceFlow.Domain.Entities;

namespace FinanceFlow.Application.DTOs;

/// <summary>Alteração em lote: só os campos informados são aplicados a todos os lançamentos selecionados.</summary>
public record BatchUpdateTransactionsRequestDto(
    IReadOnlyList<Guid> Ids,
    Guid? CategoryId = null,
    Guid? AccountId = null,
    TransactionStatus? Status = null);

public record BatchDeleteTransactionsRequestDto(IReadOnlyList<Guid> Ids);

/// <summary>Um lançamento que ficou de fora do lote, com o motivo para a tela explicar ao usuário.</summary>
public record BatchTransactionSkippedDto(Guid Id, string Description, string Reason);

/// <summary>Resultado de uma operação em lote: o que foi aplicado e o que ficou de fora (sem derrubar o restante).</summary>
public record BatchTransactionResultDto(
    int Requested,
    int Succeeded,
    IReadOnlyList<BatchTransactionSkippedDto> Skipped);
