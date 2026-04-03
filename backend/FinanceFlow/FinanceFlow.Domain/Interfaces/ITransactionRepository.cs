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

    /// <summary>
    /// Retorna transações Scheduled ou IsRecurring de todos os usuários
    /// com Date igual à data alvo — uso interno do Worker.
    /// </summary>
    Task<IEnumerable<Transaction>> GetDueTransactionsAsync( // adicionado
        DateTime targetDate, // adicionado
        CancellationToken cancellationToken = default); // adicionado

    Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default);
    Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default);
    Task DeleteAsync(Transaction transaction, CancellationToken cancellationToken = default);
}
