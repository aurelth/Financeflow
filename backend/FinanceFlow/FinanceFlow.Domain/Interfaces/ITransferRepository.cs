using FinanceFlow.Domain.Entities;

namespace FinanceFlow.Domain.Interfaces;

/// <summary>
/// Acesso a vínculos de transferência e aos lançamentos candidatos a pareamento.
/// Os métodos Add/Remove só registram a mudança: quem chama decide o momento do
/// <see cref="SaveChangesAsync"/>, o que permite gravar as duas pontas e o vínculo de forma atômica.
/// </summary>
public interface ITransferRepository
{
    /// <summary>
    /// Lançamentos sem vínculo que podem ser a outra ponta de uma transferência:
    /// receitas, despesas e transferências com direção, de qualquer conta exceto <paramref name="excludeAccountId"/>.
    /// </summary>
    Task<IReadOnlyList<Transaction>> GetCandidatesAsync(
        Guid userId,
        Guid? excludeAccountId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    /// <summary>Pontas de transferência (com direção) que ainda não têm par.</summary>
    Task<IReadOnlyList<Transaction>> GetUnlinkedTransfersAsync(
        Guid userId,
        int limit,
        CancellationToken cancellationToken = default);

    /// <summary>Lançamentos rastreados (editáveis) do usuário, ignorando os excluídos.</summary>
    Task<IReadOnlyList<Transaction>> GetTrackedTransactionsAsync(
        Guid userId,
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default);

    /// <summary>Vínculo com as duas pontas carregadas (rastreadas), contas incluídas.</summary>
    Task<TransferLink?> GetLinkAsync(
        Guid linkId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>Vínculo ao qual o lançamento pertence, se houver.</summary>
    Task<TransferLink?> GetLinkByTransactionAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default);

    Task<bool> IsLinkedAsync(Guid transactionId, CancellationToken cancellationToken = default);

    /// <summary>Pares que o usuário já rejeitou, para os lançamentos informados (ids da outra ponta).</summary>
    Task<IReadOnlyList<TransferRejection>> GetRejectionsAsync(
        Guid userId,
        IReadOnlyCollection<Guid> transactionIds,
        CancellationToken cancellationToken = default);

    void AddTransaction(Transaction transaction);
    void AddLink(TransferLink link);
    void RemoveLink(TransferLink link);
    void AddRejection(TransferRejection rejection);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
