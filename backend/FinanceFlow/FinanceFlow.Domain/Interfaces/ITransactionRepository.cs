using FinanceFlow.Domain.Entities;

namespace FinanceFlow.Domain.Interfaces;

public interface ITransactionRepository
{
    /// <summary>
    /// Retorna transações paginadas do utilizador com filtros opcionais.
    /// </summary>
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

    /// <summary>
    /// Retorna uma transação pelo Id, validando que pertence ao utilizador.
    /// </summary>
    Task<Transaction?> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Transaction transaction,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Transaction transaction,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Transaction transaction,
        CancellationToken cancellationToken = default);
}
