using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.ValueObjects;

namespace FinanceFlow.Domain.Interfaces;

public interface IBankImportRepository
{
    Task<BankImport?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<BankImport>> GetAllByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(BankImport bankImport, CancellationToken cancellationToken = default);
    Task UpdateAsync(BankImport bankImport, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retorna os lançamentos já importados na conta cujo hash ou FITID estão entre os informados.
    /// Ignora lançamentos excluídos (soft delete), permitindo reimportar o que foi apagado.
    /// </summary>
    Task<IReadOnlyList<ImportedTransactionKey>> GetImportedKeysAsync(
        Guid userId,
        Guid accountId,
        IReadOnlyCollection<string> hashes,
        IReadOnlyCollection<string> externalIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retorna lançamentos manuais (sem origem em importação) da conta no período,
    /// candidatos a corresponder a linhas do extrato.
    /// </summary>
    Task<IReadOnlyList<Transaction>> GetManualCandidatesAsync(
        Guid userId,
        Guid accountId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    /// <summary>Retorna lançamentos do usuário pelos ids (para exibir a correspondência no preview).</summary>
    Task<IReadOnlyList<Transaction>> GetTransactionsByIdsAsync(
        Guid userId,
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retorna a importação confirmada mais recente da conta que trouxe o saldo do extrato
    /// (maior data de saldo; empate pela criação). Não rastreada e sem os lançamentos.
    /// </summary>
    Task<BankImport?> GetLatestStatementAsync(
        Guid userId,
        Guid accountId,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
}
