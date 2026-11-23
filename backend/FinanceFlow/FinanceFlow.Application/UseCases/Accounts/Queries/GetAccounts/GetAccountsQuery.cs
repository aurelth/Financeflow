using FinanceFlow.Application.DTOs.Accounts;
using MediatR;

namespace FinanceFlow.Application.UseCases.Accounts.Queries.GetAccounts;

public record GetAccountsQuery(
    Guid UserId,
    bool IncludeArchived = false
) : IRequest<IEnumerable<AccountDto>>;
