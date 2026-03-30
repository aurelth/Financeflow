using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using FinanceFlow.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace FinanceFlow.Infrastructure.Persistence.Repositories;

public class BudgetRepository(FinanceFlowDbContext context) : IBudgetRepository
{
    public async Task<IEnumerable<Budget>> GetByUserAndPeriodAsync(
        Guid userId,
        int month,
        int year,
        CancellationToken cancellationToken = default) =>
        await context.Budgets
            .Include(b => b.Category)
            .Where(b => b.UserId == userId && b.Month == month && b.Year == year)
            .OrderBy(b => b.Category.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<Budget?> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        await context.Budgets
            .Include(b => b.Category)
            .Where(b => b.Id == id && b.UserId == userId)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<bool> ExistsAsync(
        Guid userId,
        Guid categoryId,
        int month,
        int year,
        CancellationToken cancellationToken = default) =>
        await context.Budgets
            .AnyAsync(b =>
                b.UserId == userId &&
                b.CategoryId == categoryId &&
                b.Month == month &&
                b.Year == year,
                cancellationToken);

    public async Task AddAsync(
        Budget budget,
        CancellationToken cancellationToken = default)
    {
        await context.Budgets.AddAsync(budget, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        Budget budget,
        CancellationToken cancellationToken = default)
    {
        context.Budgets.Update(budget);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        Budget budget,
        CancellationToken cancellationToken = default)
    {
        context.Budgets.Remove(budget);
        await context.SaveChangesAsync(cancellationToken);
    }
}
