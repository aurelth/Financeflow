using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using FinanceFlow.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace FinanceFlow.Infrastructure.Persistence.Repositories;

public class CategoryRuleRepository(FinanceFlowDbContext context) : ICategoryRuleRepository
{
    public async Task<IReadOnlyList<CategoryRule>> GetByKeysAsync(
        Guid userId,
        IReadOnlyCollection<string> keys,
        CancellationToken cancellationToken = default) =>
        await context.CategoryRules
            .AsNoTracking()
            .Where(r => r.UserId == userId && keys.Contains(r.Key))
            .ToListAsync(cancellationToken);

    // Sem o filtro de categoria excluída: a regra de uma categoria apagada ainda ocupa o índice único
    // e precisa ser reaproveitada em vez de duplicada.
    public async Task<IReadOnlyList<CategoryRule>> GetTrackedByKeysAsync(
        Guid userId,
        IReadOnlyCollection<string> keys,
        CancellationToken cancellationToken = default) =>
        await context.CategoryRules
            .IgnoreQueryFilters()
            .Where(r => r.DeletedAt == null && r.UserId == userId && keys.Contains(r.Key))
            .ToListAsync(cancellationToken);

    // A categoria pode ter sido excluída (soft delete) depois de a regra nascer: nesse caso a regra some da lista
    public async Task<IReadOnlyList<CategoryRule>> GetAllByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        await context.CategoryRules
            .AsNoTracking()
            .Include(r => r.Category)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.HitCount)
            .ThenByDescending(r => r.LastUsedAt)
            .ToListAsync(cancellationToken);

    public async Task<CategoryRule?> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        await context.CategoryRules
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId, cancellationToken);

    public void Add(CategoryRule rule) => context.CategoryRules.Add(rule);

    public void Remove(CategoryRule rule) => context.CategoryRules.Remove(rule);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
