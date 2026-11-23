using FinanceFlow.Application.DTOs;
using MediatR;

namespace FinanceFlow.Application.UseCases.Dashboard.Queries.GetDashboardSummary;

public record GetDashboardSummaryQuery(
    Guid UserId,
    int Month,
    int Year,
    Guid? AccountId = null
) : IRequest<DashboardSummaryDto>;
