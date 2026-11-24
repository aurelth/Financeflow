using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.DTOs.Accounts;
using FinanceFlow.Application.Services;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Accounts.Queries.GetAccountReconciliation;

public class GetAccountReconciliationQueryHandler(
    IAccountRepository accountRepository,
    IAccountReconciliationService reconciliationService)
    : IRequestHandler<GetAccountReconciliationQuery, AccountReconciliationDto>
{
    public async Task<AccountReconciliationDto> Handle(
        GetAccountReconciliationQuery request,
        CancellationToken cancellationToken)
    {
        var account = await accountRepository.GetByIdAsync(
            request.AccountId, request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), request.AccountId);

        return await reconciliationService.ReconcileAsync(account, cancellationToken);
    }
}
