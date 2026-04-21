using FinanceFlow.Application.DTOs.Goals;

namespace FinanceFlow.Application.Common.Interfaces;

public interface IGoalProgressService
{
    Task<GoalsSummaryResultDto> CalculateAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
