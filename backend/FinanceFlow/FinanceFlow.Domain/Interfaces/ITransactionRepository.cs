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
        Guid? accountId,
        CancellationToken cancellationToken = default);

    Task<Transaction?> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lançamentos (não excluídos) do usuário com os ids informados, sem rastreamento e com o vínculo de
    /// transferência carregado. Ids inexistentes ou de outro usuário simplesmente não voltam. Alimenta as operações em lote.
    /// </summary>
    Task<List<Transaction>> GetByIdsAsync(
        Guid userId,
        IReadOnlyCollection<Guid> ids,
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
    /// Retorna os lançamentos ainda não pagos (Agendados ou Pendentes, exceto transferências) de todos os
    /// usuários com Date igual à data alvo — uso interno do Worker. Um lançamento já pago não gera lembrete.
    /// </summary>
    Task<IEnumerable<Transaction>> GetDueTransactionsAsync(
        DateTime targetDate,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<Transaction>> GetFutureRecurringAsync(
        Guid recurrenceGroupId,
        DateTime fromDate,
        CancellationToken cancellationToken = default);

    /// <summary>Todas as parcelas (não excluídas) de uma compra parcelada do usuário, em ordem de número.</summary>
    Task<List<Transaction>> GetInstallmentGroupAsync(
        Guid installmentGroupId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Parcelas seguintes à informada (número maior). Com <paramref name="onlyUnpaid"/>, deixa de fora as já pagas,
    /// que representam dinheiro que já saiu e não devem ser alteradas em massa.
    /// </summary>
    Task<List<Transaction>> GetFutureInstallmentsAsync(
        Guid installmentGroupId,
        Guid userId,
        int afterNumber,
        bool onlyUnpaid,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lançamentos que servem de base para detectar recorrências: desde <paramref name="from"/>, sem transferências
    /// nem parcelas. Traz os já recorrentes (para a detecção não duplicar uma série existente) e deixa de fora
    /// os agendados avulsos. Somente leitura, com categoria e conta para a tela.
    /// </summary>
    Task<List<Transaction>> GetForRecurrenceAnalysisAsync(
        Guid userId,
        DateTime from,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lançamentos de um período (transferências ficam de fora), com categoria e conta, em ordem de data.
    /// Somente leitura — alimenta o calendário.
    /// </summary>
    Task<List<Transaction>> GetByPeriodAsync(
        Guid userId,
        DateTime from,
        DateTime toExclusive,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lançamentos ainda não pagos com data de <paramref name="since"/> até antes de <paramref name="before"/>
    /// (transferências ficam de fora), do mais antigo ao mais novo, limitados a <paramref name="limit"/>.
    /// </summary>
    Task<List<Transaction>> GetUnpaidBeforeAsync(
        Guid userId,
        DateTime before,
        DateTime since,
        int limit,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Transforma lançamentos em uma série recorrente: atualiza os existentes (vínculo com o grupo) e grava as
    /// ocorrências futuras, tudo num único SaveChanges.
    /// </summary>
    Task SaveRecurringSeriesAsync(
        IEnumerable<Transaction> linked,
        IEnumerable<Transaction> projected,
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

    /// <summary>Grava todas as transações num único SaveChanges: ou entram todas, ou nenhuma.</summary>
    Task AddRangeAsync(IEnumerable<Transaction> transactions, CancellationToken cancellationToken = default);

    /// <summary>Atualiza todas num único SaveChanges (usado em edição/cancelamento de grupos).</summary>
    Task UpdateRangeAsync(IEnumerable<Transaction> transactions, CancellationToken cancellationToken = default);
    Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default);
    Task DeleteAsync(Transaction transaction, CancellationToken cancellationToken = default);
}
