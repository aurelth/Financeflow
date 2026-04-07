using AutoMapper;
using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.Common.Interfaces; // Adicionado
using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using MediatR;
using System.Text.Json;

namespace FinanceFlow.Application.UseCases.Transactions.Commands.UpdateTransaction;

public class UpdateTransactionCommandHandler(
    ITransactionRepository transactionRepository,
    ICategoryRepository categoryRepository,
    ICacheService cache, // Adicionado
    IMapper mapper)
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

        // Verifica se Amount, Description, CategoryId ou SubcategoryId foram alterados
        var amountAlterado = transaction.Amount != request.Amount;
        var descricaoAlterada = transaction.Description != request.Description;
        var categoriaAlterada = transaction.CategoryId != request.CategoryId;
        var subcategoriaAlterada = transaction.SubcategoryId != request.SubcategoryId;

        var devePropagarParaFuturas = request.PropagateToFuture
            && transaction.RecurrenceGroupId.HasValue
            && (amountAlterado || descricaoAlterada || categoriaAlterada || subcategoriaAlterada);

        // Guarda a data original antes de alterar para invalidar o cache do mês correto
        var dataOriginal = transaction.Date; // Adicionado

        // Atualiza a transação atual
        transaction.Amount = request.Amount;
        transaction.Type = request.Type;
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

        // Adicionado: invalida o cache do dashboard para o mês da transação
        await InvalidarCacheDashboardAsync(request.UserId, dataOriginal, cancellationToken);

        // Adicionado: invalida também para o novo mês caso a data tenha mudado
        if (request.Date.Month != dataOriginal.Month || request.Date.Year != dataOriginal.Year)
            await InvalidarCacheDashboardAsync(request.UserId, request.Date, cancellationToken);

        var updated = await transactionRepository.GetByIdAsync(
            transaction.Id, request.UserId, cancellationToken);

        return mapper.Map<TransactionDto>(updated);
    }

    // Adicionado: invalida todas as chaves de cache do dashboard para o mês/ano da transação
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
