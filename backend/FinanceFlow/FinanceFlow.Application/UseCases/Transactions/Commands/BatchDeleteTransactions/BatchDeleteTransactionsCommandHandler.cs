using FinanceFlow.Application.DTOs;
using FinanceFlow.Application.Services;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Transactions.Commands.BatchDeleteTransactions;

public class BatchDeleteTransactionsCommandHandler(
    ITransactionRepository transactionRepository,
    ITransferRepository transferRepository,
    IDashboardCacheInvalidator dashboardCacheInvalidator)
    : IRequestHandler<BatchDeleteTransactionsCommand, BatchTransactionResultDto>
{
    public async Task<BatchTransactionResultDto> Handle(
        BatchDeleteTransactionsCommand request,
        CancellationToken cancellationToken)
    {
        var ids = request.Ids.Distinct().ToList();
        var transactions = await transactionRepository.GetByIdsAsync(request.UserId, ids, cancellationToken);

        var skipped = ids
            .Except(transactions.Select(t => t.Id))
            .Select(id => new BatchTransactionSkippedDto(id, string.Empty, "Lançamento não encontrado."))
            .ToList();

        // As duas pontas de uma transferência podem estar na seleção: o vínculo sai uma única vez
        var linksToRemove = new Dictionary<Guid, TransferLink>();
        var now = DateTime.UtcNow;

        foreach (var transaction in transactions)
        {
            var link = transaction.LinkAsOut ?? transaction.LinkAsIn;
            if (link is not null)
            {
                // Só a chave importa para apagar o vínculo. Um "esqueleto" evita que o Remove arraste para o
                // rastreamento o grafo carregado na leitura (as pontas da transferência, que serão atualizadas à parte)
                linksToRemove.TryAdd(link.Id, new TransferLink
                {
                    Id = link.Id,
                    UserId = link.UserId,
                    OutTransactionId = link.OutTransactionId,
                    InTransactionId = link.InTransactionId,
                });
            }

            transaction.DeletedAt = now;
        }

        // O vínculo sai no mesmo SaveChanges da exclusão, como na exclusão individual
        foreach (var link in linksToRemove.Values)
            transferRepository.RemoveLink(link);

        if (transactions.Count > 0)
        {
            await transactionRepository.UpdateRangeAsync(transactions, cancellationToken);
            await dashboardCacheInvalidator.InvalidateAsync(
                request.UserId, transactions.Select(t => t.Date), cancellationToken);
        }

        return new BatchTransactionResultDto(ids.Count, transactions.Count, skipped);
    }
}
