using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.Common.Mappings;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Application.Services;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Transactions.Commands.PayTransaction;

public class PayTransactionCommandHandler(
    ITransactionRepository transactionRepository,
    IDashboardCacheInvalidator dashboardCacheInvalidator)
    : IRequestHandler<PayTransactionCommand, TransactionDto>
{
    public async Task<TransactionDto> Handle(PayTransactionCommand request, CancellationToken cancellationToken)
    {
        var transaction = await transactionRepository.GetByIdAsync(request.Id, request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Transaction), request.Id);

        // Transferência tem as duas pontas e o vínculo gerenciados pelo fluxo de transferências
        if (transaction.Type == TransactionType.Transfer)
            throw new ValidationException("Transferências não podem ser baixadas por aqui.");

        // Duplo clique ou outra aba já deu baixa: nada a fazer
        if (transaction.Status == TransactionStatus.Paid)
            return transaction.ToDto();

        transaction.Status = TransactionStatus.Paid;

        await transactionRepository.UpdateAsync(transaction, cancellationToken);

        // O lançamento passa a contar no saldo, no dashboard e nos relatórios do mês
        await dashboardCacheInvalidator.InvalidateAsync(request.UserId, [transaction.Date], cancellationToken);

        return transaction.ToDto();
    }
}
