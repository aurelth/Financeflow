using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Application.Services;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Transactions.Commands.DeleteTransaction;

public class DeleteTransactionCommandHandler(
    ITransactionRepository transactionRepository,
    ITransferRepository transferRepository,
    IDashboardCacheInvalidator dashboardCacheInvalidator)
    : IRequestHandler<DeleteTransactionCommand>
{
    public async Task Handle(
        DeleteTransactionCommand request,
        CancellationToken cancellationToken)
    {
        var transaction = await transactionRepository.GetByIdAsync(
            request.Id, request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Transaction), request.Id);

        // Excluir uma ponta desfaz o vínculo: a outra ponta continua como transferência sem par (pendente).
        // O vínculo sai no mesmo SaveChanges da exclusão.
        var link = await transferRepository.GetLinkByTransactionAsync(transaction.Id, cancellationToken);
        if (link is not null)
            transferRepository.RemoveLink(link);

        // Soft delete da transação atual
        transaction.DeletedAt = DateTime.UtcNow;
        await transactionRepository.UpdateAsync(transaction, cancellationToken);

        var datasAfetadas = new List<DateTime> { transaction.Date };

        // Soft delete das futuras do mesmo grupo (recorrência ou compra parcelada)
        if (request.DeleteFuture && (transaction.RecurrenceGroupId.HasValue || transaction.InstallmentGroupId.HasValue))
        {
            // Parcelas: as seguintes por número, só as ainda não pagas (parcela paga já saiu do bolso e fica no histórico)
            var futuras = transaction.InstallmentGroupId.HasValue
                ? await transactionRepository.GetFutureInstallmentsAsync(
                    transaction.InstallmentGroupId.Value,
                    request.UserId,
                    transaction.InstallmentNumber!.Value,
                    onlyUnpaid: true,
                    cancellationToken)
                : (await transactionRepository.GetFutureRecurringAsync(
                    transaction.RecurrenceGroupId!.Value,
                    transaction.Date,
                    cancellationToken)).ToList();

            var agora = DateTime.UtcNow;
            foreach (var futura in futuras)
            {
                futura.DeletedAt = agora;
                datasAfetadas.Add(futura.Date);
            }

            await transactionRepository.UpdateRangeAsync(futuras, cancellationToken);
        }

        // Invalida o cache do dashboard de cada mês tocado
        await dashboardCacheInvalidator.InvalidateAsync(transaction.UserId, datasAfetadas, cancellationToken);
    }
}
