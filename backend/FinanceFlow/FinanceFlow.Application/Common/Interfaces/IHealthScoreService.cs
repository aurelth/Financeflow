using FinanceFlow.Application.DTOs.HealthScore;

namespace FinanceFlow.Application.Common.Interfaces;

public interface IHealthScoreService
{
    Task<HealthScoreResult> CalculateAsync(
        Guid userId,
        int month,
        int year,
        CancellationToken cancellationToken = default);
}
