using FinanceFlow.Domain.Entities;

namespace FinanceFlow.Domain.Interfaces;

public interface ICategoryRepository
{
    /// <summary>
    /// Retorna todas as categorias visíveis para o utilizador:
    /// categorias padrão do sistema (UserId = null) + categorias do próprio utilizador.
    /// </summary>
    Task<IEnumerable<Category>> GetAllByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retorna uma categoria pelo Id, validando que pertence ao utilizador
    /// ou é uma categoria padrão do sistema.
    /// </summary>
    Task<Category?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se já existe uma categoria com o mesmo nome para o utilizador.
    /// </summary>
    Task<bool> ExistsByNameAsync(string name, Guid userId, TransactionType type, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se a categoria tem transações vinculadas.
    /// Usado para decidir entre soft delete ou remoção física.
    /// </summary>
    Task<bool> HasTransactionsAsync(Guid categoryId, CancellationToken cancellationToken = default);

    Task AddAsync(Category category, CancellationToken cancellationToken = default);
    Task UpdateAsync(Category category, CancellationToken cancellationToken = default);
    Task DeleteAsync(Category category, CancellationToken cancellationToken = default);
}
