using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Application.DTOs.HealthScore;
using MediatR;

namespace FinanceFlow.Application.UseCases.HealthScore.Queries.GetHealthScore;

public class GetHealthScoreQueryHandler(IHealthScoreService healthScoreService)
    : IRequestHandler<GetHealthScoreQuery, HealthScoreResultDto>
{
    public async Task<HealthScoreResultDto> Handle(
        GetHealthScoreQuery request,
        CancellationToken cancellationToken)
        => await healthScoreService.CalculateAsync(
            request.UserId,
            request.Month,
            request.Year,
            cancellationToken);
}
