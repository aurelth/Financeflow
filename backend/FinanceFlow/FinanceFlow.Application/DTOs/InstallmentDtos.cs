using FinanceFlow.Domain.Entities;

namespace FinanceFlow.Application.DTOs;

/// <summary>Uma parcela do plano de uma compra parcelada.</summary>
public record InstallmentDto(
    Guid Id,
    int Number,
    DateTime Date,
    decimal Amount,
    TransactionStatus Status);

/// <summary>
/// Plano de uma compra parcelada. Os totais consideram as parcelas que ainda existem: parcelas canceladas
/// saem da conta, parcelas pagas contam em <see cref="PaidAmount"/>.
/// </summary>
public record InstallmentPlanDto(
    Guid GroupId,
    string Description,
    TransactionType Type,
    int Count,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal RemainingAmount,
    IReadOnlyList<InstallmentDto> Installments);

/// <summary>Resultado do cancelamento de uma compra parcelada.</summary>
public record CancelInstallmentsResultDto(int CancelledCount, int KeptPaidCount);
