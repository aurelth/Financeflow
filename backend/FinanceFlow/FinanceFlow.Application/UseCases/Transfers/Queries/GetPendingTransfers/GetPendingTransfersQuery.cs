using FinanceFlow.Application.DTOs.Transfers;
using MediatR;

namespace FinanceFlow.Application.UseCases.Transfers.Queries.GetPendingTransfers;

public record GetPendingTransfersQuery(Guid UserId) : IRequest<IReadOnlyList<PendingTransferDto>>;
