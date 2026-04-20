using FinanceFlow.Application.DTOs.HealthScore;
using MediatR;

namespace FinanceFlow.Application.UseCases.HealthScore.Queries.GetHealthScore;

public record GetHealthScoreQuery(
    Guid UserId,
    int Month,
    int Year
) : IRequest<HealthScoreResult>;
