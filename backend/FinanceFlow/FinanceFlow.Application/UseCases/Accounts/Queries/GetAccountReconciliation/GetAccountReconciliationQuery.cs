using FinanceFlow.Application.DTOs.Accounts;
using MediatR;

namespace FinanceFlow.Application.UseCases.Accounts.Queries.GetAccountReconciliation;

public record GetAccountReconciliationQuery(Guid AccountId, Guid UserId) : IRequest<AccountReconciliationDto>;
