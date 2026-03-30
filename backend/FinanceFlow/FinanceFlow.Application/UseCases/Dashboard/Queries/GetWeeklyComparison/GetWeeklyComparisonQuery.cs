using FinanceFlow.Application.DTOs;
using MediatR;

namespace FinanceFlow.Application.UseCases.Dashboard.Queries.GetWeeklyComparison;

public record GetWeeklyComparisonQuery(
    Guid UserId,
    int Month,
    int Year
) : IRequest<IEnumerable<WeeklyComparisonDto>>;
