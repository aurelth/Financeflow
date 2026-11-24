using FinanceFlow.Domain.Enums;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using FinanceFlow.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace FinanceFlow.Infrastructure.Persistence.Repositories;

public class AccountRepository(FinanceFlowDbContext context) : IAccountRepository
{
    public async Task<IEnumerable<Account>> GetByUserAsync(
        Guid userId,
        bool includeArchived,
        CancellationToken cancellationToken = default)
    {
        var query = context.Accounts.Where(a => a.UserId == userId);

        if (!includeArchived)
            query = query.Where(a => !a.IsArchived);

        return await query
            .OrderByDescending(a => a.IsDefault)
            .ThenBy(a => a.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Account?> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        await context.Accounts
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, cancellationToken);

    public async Task<Account?> GetByKeyAsync(
        Guid userId,
        string? bankId,
        string accountKeyHash,
        CancellationToken cancellationToken = default) =>
        await context.Accounts
            .FirstOrDefaultAsync(a =>
                a.UserId == userId &&
                a.BankId == bankId &&
                a.AccountKeyHash == accountKeyHash,
                cancellationToken);

    public async Task<Account> GetOrCreateDefaultAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var existing = await context.Accounts
            .FirstOrDefaultAsync(a => a.UserId == userId && a.IsDefault, cancellationToken);

        if (existing is not null)
            return existing;

        var account = Account.CreateDefault(userId);

        try
        {
            await context.Accounts.AddAsync(account, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return account;
        }
        catch (DbUpdateException)
        {
            // Outra requisição criou a conta principal ao mesmo tempo: usa a que ficou
            context.Entry(account).State = EntityState.Detached;
            return await context.Accounts
                .FirstAsync(a => a.UserId == userId && a.IsDefault, cancellationToken);
        }
    }

    public async Task<IReadOnlyDictionary<Guid, decimal>> GetMovementTotalsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var rows = await context.Transactions
            .IgnoreQueryFilters()
            .Where(t =>
                t.UserId == userId &&
                t.DeletedAt == null &&
                t.Status != TransactionStatus.Scheduled &&
                (t.Account.InitialBalanceDate == null || t.Date > t.Account.InitialBalanceDate))
            .GroupBy(t => t.AccountId)
            .Select(g => new
            {
                AccountId = g.Key,
                Total = g.Sum(t =>
                    t.Type == TransactionType.Income ? t.Amount :
                    t.Type == TransactionType.Expense ? -t.Amount :
                    // Transferência entra (+) ou sai (-) da conta; sem direção (legado) não move o saldo
                    t.Type == TransactionType.Transfer && t.TransferDirection == TransferDirection.In ? t.Amount :
                    t.Type == TransactionType.Transfer && t.TransferDirection == TransferDirection.Out ? -t.Amount :
                    0m),
            })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(r => r.AccountId, r => r.Total);
    }

    public async Task<bool> NameExistsAsync(
        Guid userId,
        string name,
        Guid? excludeId,
        CancellationToken cancellationToken = default) =>
        await context.Accounts.AnyAsync(a =>
            a.UserId == userId &&
            a.Name == name &&
            (excludeId == null || a.Id != excludeId),
            cancellationToken);

    public async Task AddAsync(
        Account account,
        CancellationToken cancellationToken = default)
    {
        await context.Accounts.AddAsync(account, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        Account account,
        CancellationToken cancellationToken = default)
    {
        context.Accounts.Update(account);
        await context.SaveChangesAsync(cancellationToken);
    }
}
