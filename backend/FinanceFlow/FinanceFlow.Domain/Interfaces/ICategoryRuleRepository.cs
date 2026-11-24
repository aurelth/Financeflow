using FinanceFlow.Domain.Entities;

namespace FinanceFlow.Domain.Interfaces;

/// <summary>
/// Regras de categorização aprendidas. Add/Remove só registram a mudança;
/// quem chama decide o momento do <see cref="SaveChangesAsync"/>.
/// </summary>
public interface ICategoryRuleRepository
{
    /// <summary>Regras do usuário para as chaves informadas (somente leitura).</summary>
    Task<IReadOnlyList<CategoryRule>> GetByKeysAsync(
        Guid userId,
        IReadOnlyCollection<string> keys,
        CancellationToken cancellationToken = default);

    /// <summary>Regras do usuário para as chaves informadas, rastreadas para atualização.</summary>
    Task<IReadOnlyList<CategoryRule>> GetTrackedByKeysAsync(
        Guid userId,
        IReadOnlyCollection<string> keys,
        CancellationToken cancellationToken = default);

    /// <summary>Todas as regras do usuário, com a categoria, da mais usada para a menos usada.</summary>
    Task<IReadOnlyList<CategoryRule>> GetAllByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<CategoryRule?> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);

    void Add(CategoryRule rule);
    void Remove(CategoryRule rule);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
