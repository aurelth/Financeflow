using FinanceFlow.Domain.Enums;

namespace FinanceFlow.Application.DTOs.Accounts;

/// <summary>
/// Comparação entre o saldo que o banco informou no último extrato confirmado (LEDGERBAL)
/// e o saldo que o FinanceFlow calcula para a conta na mesma data.
/// </summary>
public record AccountReconciliationDto(
    Guid AccountId,
    ReconciliationStatus Status,
    Guid? ImportId,
    string? FileName,
    DateTime? StatementDate,
    DateTime? StatementStart,
    DateTime? StatementEnd,
    decimal? StatementBalance,
    decimal? CalculatedBalance,
    /// <summary>Saldo do extrato menos o saldo calculado: positivo = faltam entradas no FinanceFlow.</summary>
    decimal? Difference,
    string? Message);
