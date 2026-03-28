using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Transactions.Commands.RemoveAttachment;

public class RemoveAttachmentCommandHandler(
    ITransactionRepository transactionRepository,
    IAttachmentService attachmentService)
    : IRequestHandler<RemoveAttachmentCommand>
{
    public async Task Handle(
        RemoveAttachmentCommand request,
        CancellationToken cancellationToken)
    {
        var transaction = await transactionRepository.GetByIdAsync(
            request.Id, request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Transaction), request.Id);

        if (string.IsNullOrEmpty(transaction.AttachmentPath))
            return;

        // Remove o ficheiro do disco
        await attachmentService.DeleteAsync(transaction.AttachmentPath);

        // Limpa o caminho na base de dados
        transaction.AttachmentPath = null;
        await transactionRepository.UpdateAsync(transaction, cancellationToken);
    }
}
