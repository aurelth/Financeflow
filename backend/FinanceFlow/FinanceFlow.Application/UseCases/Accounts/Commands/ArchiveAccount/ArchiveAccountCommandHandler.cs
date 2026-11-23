using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Accounts.Commands.ArchiveAccount;

public class ArchiveAccountCommandHandler(IAccountRepository accountRepository)
    : IRequestHandler<ArchiveAccountCommand>
{
    public async Task Handle(
        ArchiveAccountCommand request,
        CancellationToken cancellationToken)
    {
        var account = await accountRepository.GetByIdAsync(
            request.Id, request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), request.Id);

        if (account.IsDefault)
            throw new ValidationException("A conta principal não pode ser arquivada.");

        if (account.IsArchived)
            return;

        account.IsArchived = true;
        await accountRepository.UpdateAsync(account, cancellationToken);
    }
}
