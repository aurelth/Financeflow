using FinanceFlow.Application.DTOs.Transfers;
using MediatR;

namespace FinanceFlow.Application.UseCases.Transfers.Commands.LinkTransfers;

public record LinkTransfersCommand(
    Guid UserId,
    Guid FirstTransactionId,
    Guid SecondTransactionId
) : IRequest<TransferLinkDto>;
