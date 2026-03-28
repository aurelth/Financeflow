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

        // Soft delete
        transaction.DeletedAt = DateTime.UtcNow;

        await transactionRepository.UpdateAsync(transaction, cancellationToken);
    }
}
