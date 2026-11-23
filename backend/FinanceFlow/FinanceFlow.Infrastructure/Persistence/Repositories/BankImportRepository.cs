using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using FinanceFlow.Domain.ValueObjects;
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
            .Include(b => b.Account)
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
            .Include(b => b.Account)
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

    public async Task<IReadOnlyList<ImportedTransactionKey>> GetImportedKeysAsync(
        Guid userId,
        Guid accountId,
        IReadOnlyCollection<string> hashes,
        IReadOnlyCollection<string> externalIds,
        CancellationToken cancellationToken = default)
    {
        if (hashes.Count == 0 && externalIds.Count == 0)
            return [];

        var hashList = hashes.ToList();
        var idList = externalIds.ToList();

        return await context.Transactions
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(t =>
                t.UserId == userId &&
                t.AccountId == accountId &&
                t.DeletedAt == null &&
                ((t.ImportHash != null && hashList.Contains(t.ImportHash)) ||
                 (t.ExternalId != null && idList.Contains(t.ExternalId))))
            .Select(t => new ImportedTransactionKey(t.Id, t.ImportHash, t.ExternalId, t.Amount))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Transaction>> GetManualCandidatesAsync(
        Guid userId,
        Guid accountId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default) =>
        await context.Transactions
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(t =>
                t.UserId == userId &&
                t.AccountId == accountId &&
                t.DeletedAt == null &&
                t.ImportHash == null &&
                t.ExternalId == null &&
                t.Type != TransactionType.Transfer &&
                t.Date >= from &&
                t.Date <= to)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Transaction>> GetTransactionsByIdsAsync(
        Guid userId,
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
            return [];

        var idList = ids.ToList();

        return await context.Transactions
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(t => t.UserId == userId && idList.Contains(t.Id))
            .ToListAsync(cancellationToken);
    }

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
