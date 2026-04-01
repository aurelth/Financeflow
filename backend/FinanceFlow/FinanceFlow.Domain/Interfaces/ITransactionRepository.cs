using FinanceFlow.Domain.Entities;

namespace FinanceFlow.Domain.Interfaces;

public interface ITransactionRepository
{
    Task<(IEnumerable<Transaction> Items, int TotalCount)> GetPagedByUserAsync(
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
        CancellationToken cancellationToken = default);

    Task<Transaction?> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se houve transações criadas ou alteradas no período após a data informada.
    /// </summary>
    Task<bool> HasChangedSinceAsync(
        Guid userId,
        int month,
        int year,
        DateTime since,
        CancellationToken cancellationToken = default);

    Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default);
    Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default);
    Task DeleteAsync(Transaction transaction, CancellationToken cancellationToken = default);
}
