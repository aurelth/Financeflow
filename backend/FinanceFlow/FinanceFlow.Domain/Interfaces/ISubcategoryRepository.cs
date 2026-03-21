using FinanceFlow.Domain.Entities;

namespace FinanceFlow.Domain.Interfaces;

public interface ISubcategoryRepository
{
    /// <summary>
    /// Retorna todas as subcategorias de uma categoria.
    /// </summary>
    Task<IEnumerable<Subcategory>> GetByCategoryIdAsync(Guid categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retorna uma subcategoria pelo Id validando que pertence à categoria informada.
    /// </summary>
    Task<Subcategory?> GetByIdAsync(Guid id, Guid categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se já existe uma subcategoria com o mesmo nome na categoria.
    /// </summary>
    Task<bool> ExistsByNameAsync(string name, Guid categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se a subcategoria tem transações vinculadas.
    /// </summary>
    Task<bool> HasTransactionsAsync(Guid subcategoryId, CancellationToken cancellationToken = default);

    Task AddAsync(Subcategory subcategory, CancellationToken cancellationToken = default);
    Task UpdateAsync(Subcategory subcategory, CancellationToken cancellationToken = default);
    Task DeleteAsync(Subcategory subcategory, CancellationToken cancellationToken = default);
}
