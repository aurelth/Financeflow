using FinanceFlow.Application.DTOs.Accounts;
using MediatR;

namespace FinanceFlow.Application.UseCases.Accounts.Queries.GetAccountsReconciliation;

/// <summary>Conciliação de todas as contas ativas do usuário (usada no dashboard e na lista de contas).</summary>
public record GetAccountsReconciliationQuery(Guid UserId) : IRequest<IEnumerable<AccountReconciliationDto>>;
