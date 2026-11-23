using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.DTOs.Accounts;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Accounts.Commands.UpdateAccount;

public class UpdateAccountCommandHandler(IAccountRepository accountRepository)
    : IRequestHandler<UpdateAccountCommand, AccountDto>
{
    public async Task<AccountDto> Handle(
        UpdateAccountCommand request,
        CancellationToken cancellationToken)
    {
        var account = await accountRepository.GetByIdAsync(
            request.Id, request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), request.Id);

        if (request.IsArchived && account.IsDefault)
            throw new ValidationException("A conta principal não pode ser arquivada.");

        var name = request.Name.Trim();

        if (await accountRepository.NameExistsAsync(request.UserId, name, account.Id, cancellationToken))
            throw new ValidationException(
                "Já existe uma conta com esse nome.",
                new Dictionary<string, string[]> { { "Name", ["Já existe uma conta com esse nome."] } });

        account.Name = name;
        account.Type = request.Type;
        account.BankName = string.IsNullOrWhiteSpace(request.BankName) ? null : request.BankName.Trim();
        account.InitialBalance = request.InitialBalance;
        account.InitialBalanceDate = request.InitialBalanceDate;
        account.Currency = string.IsNullOrWhiteSpace(request.Currency) ? account.Currency : request.Currency.ToUpperInvariant();
        account.Color = string.IsNullOrWhiteSpace(request.Color) ? account.Color : request.Color;
        account.Icon = string.IsNullOrWhiteSpace(request.Icon) ? account.Icon : request.Icon;
        account.IsArchived = request.IsArchived;

        await accountRepository.UpdateAsync(account, cancellationToken);

        var movements = await accountRepository.GetMovementTotalsAsync(request.UserId, cancellationToken);
        return AccountDto.From(account, movements.GetValueOrDefault(account.Id));
    }
}
