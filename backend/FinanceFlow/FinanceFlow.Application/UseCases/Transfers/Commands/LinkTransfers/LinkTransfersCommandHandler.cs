using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.DTOs.Transfers;
using FinanceFlow.Application.Services;
using FinanceFlow.Domain.Constants;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Transfers.Commands.LinkTransfers;

/// <summary>
/// Une dois lançamentos que já existem (a saída de uma conta e a entrada em outra) numa só transferência.
/// Receita/despesa vira transferência; o saldo de cada conta não muda, só deixa de contar como gasto/ganho.
/// </summary>
public class LinkTransfersCommandHandler(
    ITransferRepository transferRepository,
    IDashboardCacheInvalidator dashboardCacheInvalidator)
    : IRequestHandler<LinkTransfersCommand, TransferLinkDto>
{
    public async Task<TransferLinkDto> Handle(
        LinkTransfersCommand request,
        CancellationToken cancellationToken)
    {
        if (request.FirstTransactionId == request.SecondTransactionId)
            throw new ValidationException("Escolha dois lançamentos diferentes.");

        var found = (await transferRepository.GetTrackedTransactionsAsync(
                request.UserId,
                [request.FirstTransactionId, request.SecondTransactionId],
                cancellationToken))
            .ToDictionary(t => t.Id);

        var first = found.GetValueOrDefault(request.FirstTransactionId)
            ?? throw new NotFoundException(nameof(Transaction), request.FirstTransactionId);
        var second = found.GetValueOrDefault(request.SecondTransactionId)
            ?? throw new NotFoundException(nameof(Transaction), request.SecondTransactionId);

        if (first.AccountId == second.AccountId)
            throw new ValidationException("Uma transferência liga contas diferentes.");

        if (first.Amount != second.Amount)
            throw new ValidationException("Os dois lançamentos precisam ter o mesmo valor.");

        var firstDirection = TransferMatchingService.DirectionOf(first.Type, first.TransferDirection);
        var secondDirection = TransferMatchingService.DirectionOf(second.Type, second.TransferDirection);

        if (firstDirection is null || secondDirection is null)
            throw new ValidationException("Um dos lançamentos é uma transferência sem sentido (entrada ou saída) definido.");

        if (firstDirection == secondDirection)
            throw new ValidationException("Um lançamento precisa ser a saída e o outro a entrada.");

        if (await transferRepository.IsLinkedAsync(first.Id, cancellationToken) ||
            await transferRepository.IsLinkedAsync(second.Id, cancellationToken))
            throw new ValidationException("Um dos lançamentos já está vinculado a outra transferência.");

        var (outTransaction, inTransaction) = firstDirection == TransferDirection.Out
            ? (first, second)
            : (second, first);

        ConvertToTransfer(first, firstDirection.Value);
        ConvertToTransfer(second, secondDirection.Value);

        var link = new TransferLink
        {
            UserId = request.UserId,
            OutTransactionId = outTransaction.Id,
            InTransactionId = inTransaction.Id,
            Origin = TransferLinkOrigin.Manual,
        };

        transferRepository.AddLink(link);
        await transferRepository.SaveChangesAsync(cancellationToken);

        // Deixaram de ser receita/despesa: o dashboard dos meses envolvidos muda
        await dashboardCacheInvalidator.InvalidateAsync(
            request.UserId, [first.Date, second.Date], cancellationToken);

        return new TransferLinkDto(
            Id: link.Id,
            Out: TransferSideDto.From(outTransaction),
            In: TransferSideDto.From(inTransaction),
            Origin: link.Origin.ToString(),
            Score: link.Score,
            CreatedAt: link.CreatedAt);
    }

    private static void ConvertToTransfer(Transaction transaction, TransferDirection direction)
    {
        if (transaction.Type == TransactionType.Transfer)
            return;

        transaction.Type = TransactionType.Transfer;
        transaction.TransferDirection = direction;
        transaction.CategoryId = WellKnownIds.TransferCategoryId;
        transaction.SubcategoryId = null;
        transaction.UpdatedAt = DateTime.UtcNow;
    }
}
