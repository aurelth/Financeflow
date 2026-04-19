using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using FinanceFlow.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace FinanceFlow.Infrastructure.Persistence.Repositories;

public class TransactionRepository(FinanceFlowDbContext context) : ITransactionRepository
{
    public async Task<(IEnumerable<Transaction> Items, int TotalCount)> GetPagedByUserAsync(
        Guid userId,
        int page,
        int pageSize,
        DateTime? dateFrom,
        DateTime? dateTo,
        Guid? categoryId,
        Guid? subcategoryId,
        TransactionType? type,
        TransactionStatus? status,
        decimal? amountMin,
        decimal? amountMax,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var query = context.Transactions
            .IgnoreQueryFilters()
            .Include(t => t.Category)
            .Include(t => t.Subcategory)
            .Where(t =>
                t.UserId == userId &&
                t.DeletedAt == null);

        if (dateFrom.HasValue)
            query = query.Where(t => t.Date >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(t => t.Date <= dateTo.Value);

        if (categoryId.HasValue)
            query = query.Where(t => t.CategoryId == categoryId.Value);

        if (subcategoryId.HasValue)
            query = query.Where(t => t.SubcategoryId == subcategoryId.Value);

        if (type.HasValue)
            query = query.Where(t => t.Type == type.Value);

        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);

        if (amountMin.HasValue)
            query = query.Where(t => t.Amount >= amountMin.Value);

        if (amountMax.HasValue)
            query = query.Where(t => t.Amount <= amountMax.Value);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t => t.Description.Contains(search));

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<Transaction?> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        await context.Transactions
            .IgnoreQueryFilters()
            .Include(t => t.Category)
            .Include(t => t.Subcategory)
            .Where(t =>
                t.Id == id &&
                t.UserId == userId &&
                t.DeletedAt == null)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IEnumerable<Transaction>> GetDueTransactionsAsync(
    DateTime targetDate,
    CancellationToken cancellationToken = default)
    {
        var targetDateOnly = targetDate.Date;
        return await context.Transactions
            .IgnoreQueryFilters()
            .Include(t => t.User)
                .ThenInclude(u => u.NotificationPreferences)
            .Where(t =>
                t.DeletedAt == null &&
                (
                    t.Status == TransactionStatus.Scheduled ||
                    t.IsRecurring == true
                ) &&
                t.Date.Date == targetDateOnly)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasChangedSinceAsync(
        Guid userId,
        int month,
        int year,
        DateTime since,
        CancellationToken cancellationToken = default)
    {
        var dateFrom = new DateTime(year, month, 1);
        var dateTo = new DateTime(year, month, DateTime.DaysInMonth(year, month));

        return await context.Transactions
            .IgnoreQueryFilters()
            .Where(t =>
                t.UserId == userId &&
                t.DeletedAt == null &&
                t.Date >= dateFrom &&
                t.Date <= dateTo &&
                (t.CreatedAt > since || (t.UpdatedAt != null && t.UpdatedAt > since)))
            .AnyAsync(cancellationToken);
    }

    // Retorna transações futuras de um grupo recorrente a partir de uma data
    public async Task<IEnumerable<Transaction>> GetFutureRecurringAsync(
        Guid recurrenceGroupId,
        DateTime fromDate,
        CancellationToken cancellationToken = default) =>
        await context.Transactions
            .IgnoreQueryFilters()
            .Where(t =>
                t.DeletedAt == null &&
                t.RecurrenceGroupId == recurrenceGroupId &&
                t.Date > fromDate)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    // Resumo mensal para o assistente IA
    public async Task<(decimal TotalIncome, decimal TotalExpense, IEnumerable<Transaction> LastTen)> GetMonthlySummaryAsync(
        Guid userId,
        int month,
        int year,
        CancellationToken cancellationToken = default)
    {
        var dateFrom = new DateTime(year, month, 1);
        var dateTo = new DateTime(year, month, DateTime.DaysInMonth(year, month));

        var transactions = await context.Transactions
            .IgnoreQueryFilters()
            .Include(t => t.Category)
            .Where(t =>
                t.UserId == userId &&
                t.DeletedAt == null &&
                t.Date >= dateFrom &&
                t.Date <= dateTo &&
                t.Type != TransactionType.Transfer)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var totalIncome = transactions
            .Where(t => t.Type == TransactionType.Income)
            .Sum(t => t.Amount);

        var totalExpense = transactions
            .Where(t => t.Type == TransactionType.Expense)
            .Sum(t => t.Amount);

        var lastTen = transactions
            .OrderByDescending(t => t.Date)
            .Take(10)
            .ToList();

        return (totalIncome, totalExpense, lastTen);
    }

    // Top categorias de despesa para o assistente IA
    public async Task<IEnumerable<(string CategoryName, decimal TotalAmount)>> GetTopExpenseCategoriesAsync(
        Guid userId,
        int month,
        int year,
        int top = 5,
        CancellationToken cancellationToken = default)
    {
        var dateFrom = new DateTime(year, month, 1);
        var dateTo = new DateTime(year, month, DateTime.DaysInMonth(year, month));

        var result = await context.Transactions
            .IgnoreQueryFilters()
            .Include(t => t.Category)
            .Where(t =>
                t.UserId == userId &&
                t.DeletedAt == null &&
                t.Type == TransactionType.Expense &&
                t.Date >= dateFrom &&
                t.Date <= dateTo)
            .GroupBy(t => t.Category.Name)
            .Select(g => new
            {
                CategoryName = g.Key,
                TotalAmount = g.Sum(t => t.Amount)
            })
            .OrderByDescending(x => x.TotalAmount)
            .Take(top)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return result.Select(x => (x.CategoryName, x.TotalAmount));
    }

    public async Task AddAsync(
        Transaction transaction,
        CancellationToken cancellationToken = default)
    {
        await context.Transactions.AddAsync(transaction, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        Transaction transaction,
        CancellationToken cancellationToken = default)
    {
        context.Transactions.Update(transaction);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        Transaction transaction,
        CancellationToken cancellationToken = default)
    {
        context.Transactions.Remove(transaction);
        await context.SaveChangesAsync(cancellationToken);
    }
}
