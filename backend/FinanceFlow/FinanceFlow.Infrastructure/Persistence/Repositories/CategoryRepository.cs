using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using FinanceFlow.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace FinanceFlow.Infrastructure.Persistence.Repositories;

public class CategoryRepository(FinanceFlowDbContext context) : ICategoryRepository
{
    public async Task<IEnumerable<Category>> GetAllByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        await context.Categories
            .IgnoreQueryFilters()
            .Include(c => c.Subcategories)
            .Where(c =>
                c.DeletedAt == null &&
                c.IsActive == true &&
                (c.UserId == null || c.UserId == userId))
            .OrderBy(c => c.Type)
            .ThenBy(c => c.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<Category?> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        await context.Categories
            .IgnoreQueryFilters()
            .Include(c => c.Subcategories)
            .Where(c =>
                c.Id == id &&
                c.DeletedAt == null &&
                (c.UserId == null || c.UserId == userId))
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<bool> ExistsByNameAsync(
        string name,
        Guid userId,
        TransactionType type,
        CancellationToken cancellationToken = default) =>
        await context.Categories
            .IgnoreQueryFilters()
            .AnyAsync(c =>
                c.Name == name &&
                c.Type == type &&
                c.DeletedAt == null &&
                c.UserId == userId,
            cancellationToken);

    public async Task<bool> HasTransactionsAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default) =>
        await context.Transactions
            .IgnoreQueryFilters()
            .AnyAsync(t =>
                t.CategoryId == categoryId &&
                t.DeletedAt == null,
            cancellationToken);

    public async Task AddAsync(
        Category category,
        CancellationToken cancellationToken = default)
    {
        await context.Categories.AddAsync(category, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        Category category,
        CancellationToken cancellationToken = default)
    {
        context.Categories.Update(category);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        Category category,
        CancellationToken cancellationToken = default)
    {
        context.Categories.Remove(category);
        await context.SaveChangesAsync(cancellationToken);
    }
}
