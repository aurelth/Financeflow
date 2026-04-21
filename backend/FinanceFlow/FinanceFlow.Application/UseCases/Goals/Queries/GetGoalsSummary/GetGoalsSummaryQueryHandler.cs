using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Application.DTOs.Goals;
using MediatR;

namespace FinanceFlow.Application.UseCases.Goals.Queries.GetGoalsSummary;

public class GetGoalsSummaryQueryHandler(IGoalProgressService goalProgressService)
    : IRequestHandler<GetGoalsSummaryQuery, GoalsSummaryResultDto>
{
    public async Task<GoalsSummaryResultDto> Handle(
        GetGoalsSummaryQuery request,
        CancellationToken cancellationToken)
        => await goalProgressService.CalculateAsync(request.UserId, cancellationToken);
}
