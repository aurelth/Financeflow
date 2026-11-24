using FinanceFlow.Domain.Entities;

namespace FinanceFlow.Domain.Interfaces;

/// <summary>Sugestões de recorrência dispensadas pelo usuário.</summary>
public interface IDismissedRecurrenceRepository
{
    Task<IReadOnlyList<DismissedRecurrence>> GetByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        Guid userId,
        TransactionType type,
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>Remove todas as dispensas do usuário (as sugestões voltam a aparecer). Devolve quantas removeu.</summary>
    Task<int> RemoveAllAsync(Guid userId, CancellationToken cancellationToken = default);

    Task AddAsync(DismissedRecurrence dismissed, CancellationToken cancellationToken = default);
}
