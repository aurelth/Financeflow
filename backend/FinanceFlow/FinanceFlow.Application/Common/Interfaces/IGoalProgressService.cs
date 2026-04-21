using FinanceFlow.Application.DTOs.Goals;

namespace FinanceFlow.Application.Common.Interfaces;

public interface IGoalProgressService
{
    Task<GoalsSummaryResult> CalculateAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
