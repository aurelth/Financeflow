using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Application.Services;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Transactions.Commands.CancelInstallments;

public class CancelInstallmentsCommandHandler(
    ITransactionRepository transactionRepository,
    IDashboardCacheInvalidator dashboardCacheInvalidator)
    : IRequestHandler<CancelInstallmentsCommand, CancelInstallmentsResultDto>
{
    public async Task<CancelInstallmentsResultDto> Handle(
        CancelInstallmentsCommand request,
        CancellationToken cancellationToken)
    {
        var parcels = await transactionRepository.GetInstallmentGroupAsync(
            request.GroupId, request.UserId, cancellationToken);

        if (parcels.Count == 0)
            throw new NotFoundException("Compra parcelada", request.GroupId);

        // Parcela paga é dinheiro que já saiu: fica no histórico (e no saldo). Cancelar remove só o que ainda é futuro.
        var toCancel = parcels.Where(p => p.Status != TransactionStatus.Paid).ToList();
        var keptPaid = parcels.Count - toCancel.Count;

        if (toCancel.Count > 0)
        {
            var now = DateTime.UtcNow;
            foreach (var parcel in toCancel)
                parcel.DeletedAt = now;

            // Um único SaveChanges: a compra é cancelada por inteiro ou não é cancelada
            await transactionRepository.UpdateRangeAsync(toCancel, cancellationToken);

            await dashboardCacheInvalidator.InvalidateAsync(
                request.UserId, toCancel.Select(p => p.Date), cancellationToken);
        }

        return new CancelInstallmentsResultDto(toCancel.Count, keptPaid);
    }
}
