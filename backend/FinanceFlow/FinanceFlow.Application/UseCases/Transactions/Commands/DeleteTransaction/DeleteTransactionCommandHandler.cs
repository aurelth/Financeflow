using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Transactions.Commands.DeleteTransaction;

public class DeleteTransactionCommandHandler(
    ITransactionRepository transactionRepository)
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
    }
}
