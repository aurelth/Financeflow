using FinanceFlow.Application.DTOs;
using MediatR;

namespace FinanceFlow.Application.UseCases.Transactions.Queries.GetInstallmentPlan;

public record GetInstallmentPlanQuery(Guid GroupId, Guid UserId) : IRequest<InstallmentPlanDto>;
