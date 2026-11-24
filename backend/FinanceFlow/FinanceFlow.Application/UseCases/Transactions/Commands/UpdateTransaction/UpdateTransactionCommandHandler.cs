using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;
using FinanceFlow.Domain.Interfaces;
using MediatR;
using System.Text.Json;
using FinanceFlow.Application.Common.Mappings;

namespace FinanceFlow.Application.UseCases.Transactions.Commands.UpdateTransaction;

public class UpdateTransactionCommandHandler(
    ITransactionRepository transactionRepository,
    ICategoryRepository categoryRepository,
    IAccountRepository accountRepository,
    ITransferRepository transferRepository,
    ICacheService cache)
    : IRequestHandler<UpdateTransactionCommand, TransactionDto>
{
    public async Task<TransactionDto> Handle(
        UpdateTransactionCommand request,
        CancellationToken cancellationToken)
    {
        var transaction = await transactionRepository.GetByIdAsync(
            request.Id, request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Transaction), request.Id);

        var category = await categoryRepository.GetByIdAsync(
            request.CategoryId, request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Category), request.CategoryId);

        if (category.Type != request.Type)
            throw new ValidationException(
                "O tipo da transação não coincide com o tipo da categoria.");

        // Transferência vinculada: as duas pontas se sustentam (mesmo valor, contas e sentidos opostos).
        // Alterar isso numa ponta quebraria a outra; para mudar, é preciso desfazer o vínculo antes.
        var isLinked = await transferRepository.IsLinkedAsync(transaction.Id, cancellationToken);

        if (isLinked &&
            (request.Type != transaction.Type ||
             request.Amount != transaction.Amount ||
             (request.AccountId.HasValue && request.AccountId.Value != transaction.AccountId) ||
             (request.TransferDirection.HasValue && request.TransferDirection != transaction.TransferDirection)))
        {
            throw new ValidationException(
                "Esta transferência está vinculada a um lançamento de outra conta. " +
                "Desfaça o vínculo para alterar o tipo, o valor, a conta ou o sentido.");
        }

        // Sentido: só existe em transferência. Ao virar transferência precisa ser informado;
        // ao deixar de ser, é descartado.
        TransferDirection? transferDirection = null;
        if (request.Type == TransactionType.Transfer)
        {
            transferDirection = isLinked
                ? transaction.TransferDirection
                : request.TransferDirection ?? transaction.TransferDirection;

            if (transferDirection is null)
                throw new ValidationException("Informe se a transferência é uma entrada ou uma saída desta conta.");
        }

        // Troca de conta (opcional): a conta precisa ser do usuário e não estar arquivada
        if (request.AccountId.HasValue && request.AccountId.Value != transaction.AccountId)
        {
            var account = await accountRepository.GetByIdAsync(
                request.AccountId.Value, request.UserId, cancellationToken)
                ?? throw new NotFoundException(nameof(Account), request.AccountId.Value);

            if (account.IsArchived)
                throw new ValidationException("Não é possível mover a transação para uma conta arquivada.");

            transaction.AccountId = account.Id;
        }

        // Verifica se Amount, Description, CategoryId ou SubcategoryId foram alterados
        var amountAlterado = transaction.Amount != request.Amount;
        var descricaoAlterada = transaction.Description != request.Description;
        var categoriaAlterada = transaction.CategoryId != request.CategoryId;
        var subcategoriaAlterada = transaction.SubcategoryId != request.SubcategoryId;

        var devePropagarParaFuturas = request.PropagateToFuture
            && transaction.RecurrenceGroupId.HasValue
            && (amountAlterado || descricaoAlterada || categoriaAlterada || subcategoriaAlterada);

        // Guarda a data original antes de alterar para invalidar o cache do mês correto
        var dataOriginal = transaction.Date;

        // Atualiza a transação atual
        transaction.Amount = request.Amount;
        transaction.Type = request.Type;
        transaction.TransferDirection = transferDirection;
        transaction.Date = request.Date;
        transaction.Description = request.Description;
        transaction.Status = request.Status;
        transaction.IsRecurring = request.IsRecurring;
        transaction.RecurrenceType = request.RecurrenceType;
        transaction.CategoryId = request.CategoryId;
        transaction.SubcategoryId = request.SubcategoryId;
        transaction.Tags = JsonSerializer.Serialize(request.Tags);
        transaction.Category = null!;
        transaction.Subcategory = null;
        transaction.Account = null!;

        if (request.AttachmentPath != null)
            transaction.AttachmentPath = request.AttachmentPath;
        if (request.AttachmentName != null)
            transaction.AttachmentName = request.AttachmentName;

        await transactionRepository.UpdateAsync(transaction, cancellationToken);

        // Propaga Amount, Description, CategoryId e SubcategoryId para as futuras do grupo
        if (devePropagarParaFuturas)
        {
            var futuras = await transactionRepository.GetFutureRecurringAsync(
                transaction.RecurrenceGroupId!.Value,
                transaction.Date,
                cancellationToken);

            foreach (var futura in futuras)
            {
                futura.Amount = request.Amount;
                futura.Description = request.Description;
                futura.CategoryId = request.CategoryId;
                futura.SubcategoryId = request.SubcategoryId;
                futura.Category = null!;
                futura.Subcategory = null;
                await transactionRepository.UpdateAsync(futura, cancellationToken);
            }
        }

       // Invalida o cache do dashboard para o mês da transação
        await InvalidarCacheDashboardAsync(request.UserId, dataOriginal, cancellationToken);

        // Invalida também para o novo mês caso a data tenha mudado
        if (request.Date.Month != dataOriginal.Month || request.Date.Year != dataOriginal.Year)
            await InvalidarCacheDashboardAsync(request.UserId, request.Date, cancellationToken);

        var updated = await transactionRepository.GetByIdAsync(
            transaction.Id, request.UserId, cancellationToken);

        // O registro acabou de ser gravado: se não volta na leitura, algo está errado (ex.: exclusão concorrente)
        if (updated is null)
            throw new InvalidOperationException(
                "Transação atualizada, mas não foi possível recarregá-la para montar a resposta.");

        return updated.ToDto();
    }

    // Invalida todas as chaves de cache do dashboard para o mês/ano da transação
    private async Task InvalidarCacheDashboardAsync(
        Guid userId,
        DateTime date,
        CancellationToken cancellationToken)
    {
        var prefixes = new[]
        {
            $"dashboard:summary:{userId}:{date.Year}:{date.Month}",
            $"dashboard:balance-evolution:{userId}:{date.Year}:{date.Month}",
            $"dashboard:expenses-by-category:{userId}:{date.Year}:{date.Month}",
            $"dashboard:weekly-comparison:{userId}:{date.Year}:{date.Month}",
        };

        foreach (var prefix in prefixes)
            await cache.RemoveAsync(prefix, cancellationToken);
    }
}
