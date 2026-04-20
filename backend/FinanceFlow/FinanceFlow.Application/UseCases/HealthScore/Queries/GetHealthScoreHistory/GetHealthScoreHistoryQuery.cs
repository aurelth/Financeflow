using FinanceFlow.Application.DTOs.HealthScore;
using MediatR;

namespace FinanceFlow.Application.UseCases.HealthScore.Queries.GetHealthScoreHistory;

public record GetHealthScoreHistoryQuery(
    Guid UserId
) : IRequest<IEnumerable<HealthScoreHistoryItem>>;
