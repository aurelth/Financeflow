using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.DTOs.Transfers;
using FinanceFlow.Domain.Constants;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Transfers.Commands.CreateTransfer;

/// <summary>
/// Cria uma transferência entre duas contas do usuário: a saída na origem, a entrada no destino e o vínculo,
/// tudo num único SaveChanges — nunca sobra uma ponta sem a outra por falha no meio do caminho.
/// </summary>
public class CreateTransferCommandHandler(
    IAccountRepository accountRepository,
    ITransferRepository transferRepository)
    : IRequestHandler<CreateTransferCommand, TransferLinkDto>
{
    public async Task<TransferLinkDto> Handle(
        CreateTransferCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
            throw new ValidationException("O valor da transferência deve ser maior que zero.");

        if (request.FromAccountId == request.ToAccountId)
            throw new ValidationException("A conta de origem e a de destino devem ser diferentes.");

        var from = await accountRepository.GetByIdAsync(request.FromAccountId, request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), request.FromAccountId);

        var to = await accountRepository.GetByIdAsync(request.ToAccountId, request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), request.ToAccountId);

        if (from.IsArchived || to.IsArchived)
            throw new ValidationException("Não é possível transferir de ou para uma conta arquivada.");

        var description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();

        var outTransaction = Build(request, from.Id, TransferDirection.Out,
            description ?? $"Transferência para {to.Name}");

        var inTransaction = Build(request, to.Id, TransferDirection.In,
            description ?? $"Transferência de {from.Name}");

        var link = new TransferLink
        {
            UserId = request.UserId,
            OutTransactionId = outTransaction.Id,
            InTransactionId = inTransaction.Id,
            Origin = TransferLinkOrigin.Manual,
        };

        transferRepository.AddTransaction(outTransaction);
        transferRepository.AddTransaction(inTransaction);
        transferRepository.AddLink(link);
        await transferRepository.SaveChangesAsync(cancellationToken);

        return new TransferLinkDto(
            Id: link.Id,
            Out: TransferSideDto.From(outTransaction, from.Name),
            In: TransferSideDto.From(inTransaction, to.Name),
            Origin: link.Origin.ToString(),
            Score: link.Score,
            CreatedAt: link.CreatedAt);
    }

    private static Transaction Build(
        CreateTransferCommand request,
        Guid accountId,
        TransferDirection direction,
        string description) => new()
    {
        UserId = request.UserId,
        AccountId = accountId,
        Amount = request.Amount,
        Type = TransactionType.Transfer,
        TransferDirection = direction,
        Date = request.Date,
        Description = description,
        Status = TransactionStatus.Paid,
        CategoryId = WellKnownIds.TransferCategoryId,
        Tags = "[]",
    };
}
