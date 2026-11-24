using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Transactions.Queries.GetInstallmentPlan;

public class GetInstallmentPlanQueryHandler(ITransactionRepository transactionRepository)
    : IRequestHandler<GetInstallmentPlanQuery, InstallmentPlanDto>
{
    public async Task<InstallmentPlanDto> Handle(
        GetInstallmentPlanQuery request,
        CancellationToken cancellationToken)
    {
        // A consulta já filtra por usuário: o grupo de outra pessoa é "não encontrado", não "proibido"
        var parcels = await transactionRepository.GetInstallmentGroupAsync(
            request.GroupId, request.UserId, cancellationToken);

        if (parcels.Count == 0)
            throw new NotFoundException("Compra parcelada", request.GroupId);

        var first = parcels[0];
        var paid = parcels.Where(p => p.Status == TransactionStatus.Paid).Sum(p => p.Amount);
        var total = parcels.Sum(p => p.Amount);

        return new InstallmentPlanDto(
            GroupId: request.GroupId,
            Description: first.Description,
            Type: first.Type,
            Count: first.InstallmentCount ?? parcels.Count,
            TotalAmount: total,
            PaidAmount: paid,
            RemainingAmount: total - paid,
            Installments: parcels
                .Select(p => new InstallmentDto(p.Id, p.InstallmentNumber ?? 0, p.Date, p.Amount, p.Status))
                .ToList());
    }
}
