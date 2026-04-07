using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.Common.Interfaces; // Adicionado
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Transactions.Commands.DeleteTransaction;

public class DeleteTransactionCommandHandler(
    ITransactionRepository transactionRepository,
    ICacheService cache) // Adicionado
    : IRequestHandler<DeleteTransactionCommand>
{
    public async Task Handle(
        DeleteTransactionCommand request,
        CancellationToken cancellationToken)
    {
        var transaction = await transactionRepository.GetByIdAsync(
            request.Id, request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Transaction), request.Id);

        // Soft delete da transação atual
        transaction.DeletedAt = DateTime.UtcNow;
        await transactionRepository.UpdateAsync(transaction, cancellationToken);

        // Soft delete das futuras do mesmo grupo recorrente
        if (request.DeleteFuture && transaction.RecurrenceGroupId.HasValue)
        {
            var futuras = await transactionRepository.GetFutureRecurringAsync(
                transaction.RecurrenceGroupId.Value,
                transaction.Date,
                cancellationToken);

            foreach (var futura in futuras)
            {
                futura.DeletedAt = DateTime.UtcNow;
                await transactionRepository.UpdateAsync(futura, cancellationToken);
            }
        }

        // Adicionado: invalida o cache do dashboard para o mês da transação
        await InvalidarCacheDashboardAsync(transaction.UserId, transaction.Date, cancellationToken);
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
