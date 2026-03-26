using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using FinanceFlow.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace FinanceFlow.Infrastructure.Persistence.Repositories;

public class SubcategoryRepository(FinanceFlowDbContext context) : ISubcategoryRepository
{
    public async Task<IEnumerable<Subcategory>> GetByCategoryIdAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default) =>
        await context.Subcategories
            .IgnoreQueryFilters()
            .Where(s =>
                s.CategoryId == categoryId &&
                s.DeletedAt == null &&
                s.IsActive == true)
            .OrderBy(s => s.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<Subcategory?> GetByIdAsync(
        Guid id,
        Guid categoryId,
        CancellationToken cancellationToken = default) =>
        await context.Subcategories
            .IgnoreQueryFilters()
            .Where(s =>
                s.Id == id &&
                s.CategoryId == categoryId &&
                s.DeletedAt == null)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<bool> ExistsByNameAsync(
        string name,
        Guid categoryId,
        CancellationToken cancellationToken = default) =>
        await context.Subcategories
            .IgnoreQueryFilters()
            .AnyAsync(s =>
                s.Name == name &&
                s.CategoryId == categoryId &&
                s.DeletedAt == null,
            cancellationToken);

    public async Task<bool> HasTransactionsAsync(
        Guid subcategoryId,
        CancellationToken cancellationToken = default) =>
        await context.Transactions
            .IgnoreQueryFilters()
            .AnyAsync(t =>
                t.SubcategoryId == subcategoryId &&
                t.DeletedAt == null,
            cancellationToken);

    public async Task AddAsync(
        Subcategory subcategory,
        CancellationToken cancellationToken = default)
    {
        await context.Subcategories.AddAsync(subcategory, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        Subcategory subcategory,
        CancellationToken cancellationToken = default)
    {
        context.Subcategories.Update(subcategory);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        Subcategory subcategory,
        CancellationToken cancellationToken = default)
    {
        context.Subcategories.Remove(subcategory);
        await context.SaveChangesAsync(cancellationToken);
    }
}
