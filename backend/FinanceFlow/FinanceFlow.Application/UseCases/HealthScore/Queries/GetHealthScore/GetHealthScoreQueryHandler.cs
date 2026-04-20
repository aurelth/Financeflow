using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Application.DTOs.HealthScore;
using MediatR;

namespace FinanceFlow.Application.UseCases.HealthScore.Queries.GetHealthScore;

public class GetHealthScoreQueryHandler(IHealthScoreService healthScoreService)
    : IRequestHandler<GetHealthScoreQuery, HealthScoreResult>
{
    public async Task<HealthScoreResult> Handle(
        GetHealthScoreQuery request,
        CancellationToken cancellationToken)
        => await healthScoreService.CalculateAsync(
            request.UserId,
            request.Month,
            request.Year,
            cancellationToken);
}
