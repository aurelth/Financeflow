using FinanceFlow.Application.DTOs;
using MediatR;

namespace FinanceFlow.Application.UseCases.Transactions.Commands.CancelInstallments;

/// <summary>Cancela a compra parcelada: remove as parcelas ainda não pagas e preserva as pagas.</summary>
public record CancelInstallmentsCommand(Guid GroupId, Guid UserId) : IRequest<CancelInstallmentsResultDto>;
