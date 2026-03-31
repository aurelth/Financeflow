using FinanceFlow.Domain.Entities;

namespace FinanceFlow.Domain.Interfaces;

public interface IReportRepository
{
    /// <summary>
    /// Retorna todos os relatórios do usuário ordenados por data de criação.
    /// </summary>
    Task<IEnumerable<Report>> GetByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retorna um relatório pelo Id validando que pertence ao usuário.
    /// </summary>
    Task<Report?> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retorna um relatório pelo Id sem validar usuário (uso interno do Worker).
    /// </summary>
    Task<Report?> GetByIdInternalAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Report report,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Report report,
        CancellationToken cancellationToken = default);
}
