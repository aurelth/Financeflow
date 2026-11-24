using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using FinanceFlow.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace FinanceFlow.Infrastructure.Persistence.Repositories;

public class DismissedRecurrenceRepository(FinanceFlowDbContext context) : IDismissedRecurrenceRepository
{
    public async Task<IReadOnlyList<DismissedRecurrence>> GetByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        await context.DismissedRecurrences
            .AsNoTracking()
            .Where(d => d.UserId == userId)
            .ToListAsync(cancellationToken);

    public async Task<bool> ExistsAsync(
        Guid userId,
        TransactionType type,
        string key,
        CancellationToken cancellationToken = default) =>
        await context.DismissedRecurrences
            .AnyAsync(d => d.UserId == userId && d.Type == type && d.Key == key, cancellationToken);

    public async Task<int> RemoveAllAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await context.DismissedRecurrences
            .Where(d => d.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

    public async Task AddAsync(DismissedRecurrence dismissed, CancellationToken cancellationToken = default)
    {
        await context.DismissedRecurrences.AddAsync(dismissed, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}
