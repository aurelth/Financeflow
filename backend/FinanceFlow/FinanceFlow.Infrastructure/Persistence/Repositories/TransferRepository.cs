using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using FinanceFlow.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace FinanceFlow.Infrastructure.Persistence.Repositories;

public class TransferRepository(FinanceFlowDbContext context) : ITransferRepository
{
    public async Task<IReadOnlyList<Transaction>> GetCandidatesAsync(
        Guid userId,
        Guid? excludeAccountId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default) =>
        await context.Transactions
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(t => t.Account)
            .Where(t =>
                t.UserId == userId &&
                t.DeletedAt == null &&
                t.Status != TransactionStatus.Scheduled &&
                (excludeAccountId == null || t.AccountId != excludeAccountId) &&
                t.Date >= from &&
                t.Date <= to &&
                (t.Type == TransactionType.Income ||
                 t.Type == TransactionType.Expense ||
                 (t.Type == TransactionType.Transfer && t.TransferDirection != null)) &&
                !context.TransferLinks.Any(l =>
                    l.OutTransactionId == t.Id || l.InTransactionId == t.Id))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Transaction>> GetUnlinkedTransfersAsync(
        Guid userId,
        int limit,
        CancellationToken cancellationToken = default) =>
        await context.Transactions
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(t => t.Account)
            .Where(t =>
                t.UserId == userId &&
                t.DeletedAt == null &&
                t.Type == TransactionType.Transfer &&
                t.TransferDirection != null &&
                !context.TransferLinks.Any(l =>
                    l.OutTransactionId == t.Id || l.InTransactionId == t.Id))
            .OrderByDescending(t => t.Date)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Transaction>> GetTrackedTransactionsAsync(
        Guid userId,
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();

        return await context.Transactions
            .IgnoreQueryFilters()
            .Include(t => t.Account)
            .Where(t => t.UserId == userId && t.DeletedAt == null && idList.Contains(t.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<TransferLink?> GetLinkAsync(
        Guid linkId,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        await context.TransferLinks
            .Include(l => l.OutTransaction).ThenInclude(t => t.Account)
            .Include(l => l.InTransaction).ThenInclude(t => t.Account)
            .FirstOrDefaultAsync(l => l.Id == linkId && l.UserId == userId, cancellationToken);

    public async Task<TransferLink?> GetLinkByTransactionAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default) =>
        await context.TransferLinks
            .FirstOrDefaultAsync(l =>
                l.OutTransactionId == transactionId || l.InTransactionId == transactionId,
                cancellationToken);

    public async Task<bool> IsLinkedAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default) =>
        await context.TransferLinks.AnyAsync(l =>
            l.OutTransactionId == transactionId || l.InTransactionId == transactionId,
            cancellationToken);

    public async Task<IReadOnlyList<TransferRejection>> GetRejectionsAsync(
        Guid userId,
        IReadOnlyCollection<Guid> transactionIds,
        CancellationToken cancellationToken = default)
    {
        if (transactionIds.Count == 0)
            return [];

        var idList = transactionIds.ToList();

        return await context.TransferRejections
            .AsNoTracking()
            .Where(r =>
                r.UserId == userId &&
                (idList.Contains(r.FirstTransactionId) || idList.Contains(r.SecondTransactionId)))
            .ToListAsync(cancellationToken);
    }

    public void AddTransaction(Transaction transaction) => context.Transactions.Add(transaction);

    public void AddLink(TransferLink link) => context.TransferLinks.Add(link);

    public void RemoveLink(TransferLink link) => context.TransferLinks.Remove(link);

    public void AddRejection(TransferRejection rejection)
    {
        // Rejeitar duas vezes o mesmo par não é erro: ignora se já existe
        var exists = context.TransferRejections.Local.Any(r =>
            r.UserId == rejection.UserId &&
            r.FirstTransactionId == rejection.FirstTransactionId &&
            r.SecondTransactionId == rejection.SecondTransactionId);

        if (!exists)
            context.TransferRejections.Add(rejection);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
