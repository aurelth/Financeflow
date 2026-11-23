using FinanceFlow.Application.DTOs.Accounts;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Accounts.Queries.GetAccounts;

public class GetAccountsQueryHandler(IAccountRepository accountRepository)
    : IRequestHandler<GetAccountsQuery, IEnumerable<AccountDto>>
{
    public async Task<IEnumerable<AccountDto>> Handle(
        GetAccountsQuery request,
        CancellationToken cancellationToken)
    {
        // Garante que todo usuário tenha ao menos a "Conta principal"
        await accountRepository.GetOrCreateDefaultAsync(request.UserId, cancellationToken);

        var accounts = await accountRepository.GetByUserAsync(
            request.UserId, request.IncludeArchived, cancellationToken);

        var movements = await accountRepository.GetMovementTotalsAsync(
            request.UserId, cancellationToken);

        return accounts
            .Select(a => AccountDto.From(a, movements.GetValueOrDefault(a.Id)))
            .ToList();
    }
}
