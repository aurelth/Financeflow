using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using FinanceFlow.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace FinanceFlow.Infrastructure.Persistence.Repositories;

public class BankImportRepository(FinanceFlowDbContext context) : IBankImportRepository
{
    public async Task<BankImport?> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        await context.BankImports
            .Include(b => b.Transactions)
                .ThenInclude(t => t.SuggestedCategory)
            .FirstOrDefaultAsync(b =>
                b.Id == id &&
                b.UserId == userId,
                cancellationToken);

    public async Task<IEnumerable<BankImport>> GetAllByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        await context.BankImports
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task AddAsync(
        BankImport bankImport,
        CancellationToken cancellationToken = default)
    {
        await context.BankImports.AddAsync(bankImport, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        BankImport bankImport,
        CancellationToken cancellationToken = default)
    {
        context.BankImports.Update(bankImport);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> HashExistsAsync(
        Guid userId,
        string hash,
        CancellationToken cancellationToken = default) =>
        await context.Transactions
            .IgnoreQueryFilters()
            .AnyAsync(t =>
                t.UserId == userId &&
                t.ImportHash == hash &&
                t.DeletedAt == null,
                cancellationToken);

    public async Task DeleteAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var bankImport = await context.BankImports
            .Include(b => b.Transactions)
            .FirstOrDefaultAsync(b =>
                b.Id == id &&
                b.UserId == userId,
                cancellationToken);

        if (bankImport is null)
            return;

        context.BankImports.Remove(bankImport);
        await context.SaveChangesAsync(cancellationToken);
    }
}
