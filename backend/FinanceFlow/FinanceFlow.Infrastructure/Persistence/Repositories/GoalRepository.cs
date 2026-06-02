using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using FinanceFlow.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace FinanceFlow.Infrastructure.Persistence.Repositories;

public class GoalRepository(FinanceFlowDbContext context) : IGoalRepository
{
    public async Task<IEnumerable<Goal>> GetByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        await context.Goals
            .Where(g => g.UserId == userId)
            .OrderBy(g => g.Deadline)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<Goal?> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        await context.Goals
            .Include(g => g.LinkedCategory)
            .Where(g => g.Id == id && g.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(
        Goal goal,
        CancellationToken cancellationToken = default)
    {
        await context.Goals.AddAsync(goal, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        Goal goal,
        CancellationToken cancellationToken = default)
    {
        context.Goals.Update(goal);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        Goal goal,
        CancellationToken cancellationToken = default)
    {
        context.Goals.Remove(goal);
        await context.SaveChangesAsync(cancellationToken);
    }
}
