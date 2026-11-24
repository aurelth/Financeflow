using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using FinanceFlow.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace FinanceFlow.Infrastructure.Persistence.Repositories;

public class AiUsageRepository(FinanceFlowDbContext context) : IAiUsageRepository
{
    public async Task<int> GetCountAsync(
        Guid userId,
        DateOnly day,
        CancellationToken cancellationToken = default) =>
        await context.AiUsages
            .AsNoTracking()
            .Where(u => u.UserId == userId && u.Day == day)
            .Select(u => u.Count)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<bool> TryConsumeAsync(
        Guid userId,
        DateOnly day,
        int limit,
        CancellationToken cancellationToken = default)
    {
        if (limit <= 0)
            return false;

        // Caminho comum: a linha do dia já existe. O "Count < limit" dentro do próprio UPDATE
        // é o que impede duas requisições simultâneas de passarem do limite.
        if (await IncrementAsync(userId, day, limit, cancellationToken))
            return true;

        // Zero linhas afetadas: ou o limite foi atingido, ou ainda não há linha para o dia
        var exists = await context.AiUsages
            .AnyAsync(u => u.UserId == userId && u.Day == day, cancellationToken);

        if (exists)
            return false;

        var usage = new AiUsage { UserId = userId, Day = day, Count = 1 };
        context.AiUsages.Add(usage);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            // Outra requisição criou a linha do dia no mesmo instante (índice único): é só incrementar
            context.Entry(usage).State = EntityState.Detached;
            return await IncrementAsync(userId, day, limit, cancellationToken);
        }
    }

    public async Task ReleaseAsync(
        Guid userId,
        DateOnly day,
        CancellationToken cancellationToken = default) =>
        await context.AiUsages
            .Where(u => u.UserId == userId && u.Day == day && u.Count > 0)
            .ExecuteUpdateAsync(s => s
                .SetProperty(u => u.Count, u => u.Count - 1)
                .SetProperty(u => u.UpdatedAt, DateTime.UtcNow),
                cancellationToken);

    private async Task<bool> IncrementAsync(
        Guid userId,
        DateOnly day,
        int limit,
        CancellationToken cancellationToken) =>
        await context.AiUsages
            .Where(u => u.UserId == userId && u.Day == day && u.Count < limit)
            .ExecuteUpdateAsync(s => s
                .SetProperty(u => u.Count, u => u.Count + 1)
                .SetProperty(u => u.UpdatedAt, DateTime.UtcNow),
                cancellationToken) > 0;
}
