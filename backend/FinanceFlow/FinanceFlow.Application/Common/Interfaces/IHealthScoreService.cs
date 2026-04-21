using FinanceFlow.Application.DTOs.HealthScore;

namespace FinanceFlow.Application.Common.Interfaces;

public interface IHealthScoreService
{
    Task<HealthScoreResultDto> CalculateAsync(
        Guid userId,
        int month,
        int year,
        CancellationToken cancellationToken = default);
}
