using FinanceFlow.Application.DTOs;
using MediatR;

namespace FinanceFlow.Application.UseCases.Dashboard.Queries.GetPeriodComparison;

public record GetPeriodComparisonQuery(
    Guid UserId,
    IEnumerable<(int Month, int Year)> Periods
) : IRequest<PeriodComparisonDto>;
