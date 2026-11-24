using FinanceFlow.Application.DTOs.Accounts;
using FinanceFlow.Application.Services;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Accounts.Queries.GetAccountsReconciliation;

public class GetAccountsReconciliationQueryHandler(
    IAccountRepository accountRepository,
    IAccountReconciliationService reconciliationService)
    : IRequestHandler<GetAccountsReconciliationQuery, IEnumerable<AccountReconciliationDto>>
{
    public async Task<IEnumerable<AccountReconciliationDto>> Handle(
        GetAccountsReconciliationQuery request,
        CancellationToken cancellationToken)
    {
        var accounts = await accountRepository.GetByUserAsync(
            request.UserId, includeArchived: false, cancellationToken);

        // Poucas contas por usuário: uma consulta por conta é simples e suficiente
        var results = new List<AccountReconciliationDto>();
        foreach (var account in accounts)
            results.Add(await reconciliationService.ReconcileAsync(account, cancellationToken));

        return results;
    }
}
