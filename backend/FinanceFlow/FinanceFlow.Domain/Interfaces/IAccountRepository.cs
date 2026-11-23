using FinanceFlow.Domain.Entities;

namespace FinanceFlow.Domain.Interfaces;

public interface IAccountRepository
{
    Task<IEnumerable<Account>> GetByUserAsync(
        Guid userId,
        bool includeArchived,
        CancellationToken cancellationToken = default);

    /// <summary>Retorna a conta (rastreada) se pertencer ao usuário.</summary>
    Task<Account?> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>Procura a conta do usuário pela identificação bancária do OFX.</summary>
    Task<Account?> GetByKeyAsync(
        Guid userId,
        string? bankId,
        string accountKeyHash,
        CancellationToken cancellationToken = default);

    /// <summary>Retorna a "Conta principal" do usuário, criando-a se ainda não existir.</summary>
    Task<Account> GetOrCreateDefaultAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soma, por conta, das movimentações que afetam o saldo (receitas menos despesas
    /// já pagas, a partir da data do saldo inicial quando houver).
    /// </summary>
    Task<IReadOnlyDictionary<Guid, decimal>> GetMovementTotalsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(
        Guid userId,
        string name,
        Guid? excludeId,
        CancellationToken cancellationToken = default);

    Task AddAsync(Account account, CancellationToken cancellationToken = default);
    Task UpdateAsync(Account account, CancellationToken cancellationToken = default);
}
