namespace FinanceFlow.Domain.ValueObjects;

/// <summary>
/// Dados mínimos de um lançamento já importado, usados para detectar duplicatas
/// sem carregar a entidade completa.
/// </summary>
public record ImportedTransactionKey(
    Guid TransactionId,
    string? ImportHash,
    string? ExternalId,
    decimal Amount
);
