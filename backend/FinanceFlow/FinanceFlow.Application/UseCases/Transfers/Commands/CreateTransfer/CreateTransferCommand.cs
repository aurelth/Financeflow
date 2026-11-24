using FinanceFlow.Application.DTOs.Transfers;
using MediatR;

namespace FinanceFlow.Application.UseCases.Transfers.Commands.CreateTransfer;

public record CreateTransferCommand(
    Guid UserId,
    Guid FromAccountId,
    Guid ToAccountId,
    decimal Amount,
    DateTime Date,
    string? Description
) : IRequest<TransferLinkDto>;
