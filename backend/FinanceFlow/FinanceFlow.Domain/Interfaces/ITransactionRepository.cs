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
    Task<IEnumerable<Transaction>> GetDueTransactionsAsync(
        DateTime targetDate,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<Transaction>> GetFutureRecurringAsync(
        Guid recurrenceGroupId,
        DateTime fromDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retorna resumo financeiro do utilizador para um mês/ano específico.
    /// </summary>
    Task<(decimal TotalIncome, decimal TotalExpense, IEnumerable<Transaction> LastTen)> GetMonthlySummaryAsync(
        Guid userId,
        int month,
        int year,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retorna top N categorias de despesa do utilizador para um mês/ano específico.
    /// </summary>
    Task<IEnumerable<(string CategoryName, decimal TotalAmount)>> GetTopExpenseCategoriesAsync(
        Guid userId,
        int month,
        int year,
        int top = 5,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retorna a soma das transações vinculadas a uma categoria específica.
    /// Usado para calcular o progresso das metas financeiras.
    /// </summary>
    Task<decimal> GetTotalByCategoryAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retorna a soma das transações de uma categoria num mês/ano específico.
    /// </summary>
    Task<decimal> GetTotalByCategoryAndMonthAsync(
        Guid categoryId,
        int month,
        int year,
        CancellationToken cancellationToken = default);

    Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default);
    Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default);
    Task DeleteAsync(Transaction transaction, CancellationToken cancellationToken = default);
}
