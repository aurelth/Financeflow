using FinanceFlow.Domain.Entities;

namespace FinanceFlow.Domain.Interfaces;

public interface IBudgetRepository
{
    /// <summary>
    /// Retorna todos os orçamentos do utilizador para um mês/ano específico.
    /// </summary>
    Task<IEnumerable<Budget>> GetByUserAndPeriodAsync(
        Guid userId,
        int month,
        int year,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retorna um orçamento pelo Id, validando que pertence ao utilizador.
    /// </summary>
    Task<Budget?> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se já existe um orçamento para o utilizador/categoria/mês/ano.
    /// </summary>
    Task<bool> ExistsAsync(
        Guid userId,
        Guid categoryId,
        int month,
        int year,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Budget budget,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Budget budget,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Budget budget,
        CancellationToken cancellationToken = default);
}
