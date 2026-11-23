using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.DTOs.Accounts;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Accounts.Commands.CreateAccount;

public class CreateAccountCommandHandler(IAccountRepository accountRepository)
    : IRequestHandler<CreateAccountCommand, AccountDto>
{
    public async Task<AccountDto> Handle(
        CreateAccountCommand request,
        CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();

        if (await accountRepository.NameExistsAsync(request.UserId, name, null, cancellationToken))
            throw new ValidationException(
                "Já existe uma conta com esse nome.",
                new Dictionary<string, string[]> { { "Name", ["Já existe uma conta com esse nome."] } });

        var account = new Account
        {
            UserId = request.UserId,
            Name = name,
            Type = request.Type,
            BankName = string.IsNullOrWhiteSpace(request.BankName) ? null : request.BankName.Trim(),
            InitialBalance = request.InitialBalance,
            InitialBalanceDate = request.InitialBalanceDate,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "BRL" : request.Currency.ToUpperInvariant(),
            Color = string.IsNullOrWhiteSpace(request.Color) ? "#10B981" : request.Color,
            Icon = string.IsNullOrWhiteSpace(request.Icon) ? "landmark" : request.Icon,
            IsDefault = false,
        };

        await accountRepository.AddAsync(account, cancellationToken);

        return AccountDto.From(account, movementTotal: 0m);
    }
}
