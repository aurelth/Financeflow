using FinanceFlow.Domain.Entities;

namespace FinanceFlow.Domain.Interfaces;

public interface IGoalRepository
{
    Task<IEnumerable<Goal>> GetByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Goal?> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Goal goal,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Goal goal,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Goal goal,
        CancellationToken cancellationToken = default);
}
