using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Application.Services;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Transactions.Commands.BatchUpdateTransactions;

public class BatchUpdateTransactionsCommandHandler(
    ITransactionRepository transactionRepository,
    ICategoryRepository categoryRepository,
    IAccountRepository accountRepository,
    IDashboardCacheInvalidator dashboardCacheInvalidator)
    : IRequestHandler<BatchUpdateTransactionsCommand, BatchTransactionResultDto>
{
    public async Task<BatchTransactionResultDto> Handle(
        BatchUpdateTransactionsCommand request,
        CancellationToken cancellationToken)
    {
        var ids = request.Ids.Distinct().ToList();

        // Categoria e conta de destino são entrada do usuário para o lote inteiro: se não servem, nada é feito
        Category? category = null;
        if (request.CategoryId.HasValue)
        {
            category = await categoryRepository.GetByIdAsync(request.CategoryId.Value, request.UserId, cancellationToken)
                ?? throw new NotFoundException(nameof(Category), request.CategoryId.Value);

            if (category.IsArchived || !category.IsActive)
                throw new ValidationException("Não é possível usar uma categoria arquivada ou inativa.");
        }

        Account? account = null;
        if (request.AccountId.HasValue)
        {
            account = await accountRepository.GetByIdAsync(request.AccountId.Value, request.UserId, cancellationToken)
                ?? throw new NotFoundException(nameof(Account), request.AccountId.Value);

            if (account.IsArchived)
                throw new ValidationException("Não é possível mover lançamentos para uma conta arquivada.");
        }

        var transactions = await transactionRepository.GetByIdsAsync(request.UserId, ids, cancellationToken);
        var found = transactions.ToDictionary(t => t.Id);

        var skipped = new List<BatchTransactionSkippedDto>();
        var changed = new List<Transaction>();
        var succeeded = 0;
        var affectedDates = new List<DateTime>();

        foreach (var id in ids)
        {
            // Excluído por outra aba, ou de outro usuário: tratado igual, sem revelar a diferença
            if (!found.TryGetValue(id, out var transaction))
            {
                skipped.Add(new BatchTransactionSkippedDto(id, string.Empty, "Lançamento não encontrado."));
                continue;
            }

            var reason = FindBlocker(transaction, request, category);
            if (reason is not null)
            {
                skipped.Add(new BatchTransactionSkippedDto(transaction.Id, transaction.Description, reason));
                continue;
            }

            succeeded++;

            var categoryChanges = category is not null && transaction.CategoryId != category.Id;
            var accountChanges = account is not null && transaction.AccountId != account.Id;
            var statusChanges = request.Status.HasValue && transaction.Status != request.Status.Value;

            // Já estava como pedido: conta como atendido, mas não há o que gravar
            if (!categoryChanges && !accountChanges && !statusChanges)
                continue;

            if (categoryChanges)
            {
                transaction.CategoryId = category!.Id;
                // A subcategoria pertence à categoria antiga e não faz sentido na nova
                transaction.SubcategoryId = null;
                transaction.Category = null!;
                transaction.Subcategory = null;
            }

            if (accountChanges)
            {
                transaction.AccountId = account!.Id;
                transaction.Account = null!;
            }

            if (statusChanges)
                transaction.Status = request.Status!.Value;

            changed.Add(transaction);
            affectedDates.Add(transaction.Date);
        }

        if (changed.Count > 0)
        {
            // Um único SaveChanges: o que foi aplicado entra por inteiro ou não entra
            await transactionRepository.UpdateRangeAsync(changed, cancellationToken);
            await dashboardCacheInvalidator.InvalidateAsync(request.UserId, affectedDates, cancellationToken);
        }

        return new BatchTransactionResultDto(ids.Count, succeeded, skipped);
    }

    /// <summary>Motivo pelo qual a mudança não pode ser aplicada a este lançamento; nulo quando pode.</summary>
    private static string? FindBlocker(Transaction transaction, BatchUpdateTransactionsCommand request, Category? category)
    {
        var isTransfer = transaction.Type == TransactionType.Transfer;

        if (category is not null && category.Type != transaction.Type)
            return isTransfer
                ? "Transferências não têm categoria de receita ou despesa."
                : "A categoria escolhida é de outro tipo (receita/despesa).";

        // Mover uma ponta vinculada quebraria o par (contas de origem e destino); é preciso desfazer o vínculo antes
        if (request.AccountId.HasValue &&
            request.AccountId.Value != transaction.AccountId &&
            (transaction.LinkAsOut is not null || transaction.LinkAsIn is not null))
            return "Transferência vinculada a outra conta: desfaça o vínculo para trocar a conta.";

        // Transferências têm o status gerenciado pelo fluxo de transferências
        if (request.Status.HasValue && isTransfer)
            return "O status de transferências não é alterado em lote.";

        return null;
    }
}
