using FinanceFlow.Domain.Entities;

namespace FinanceFlow.Domain.Interfaces;

public interface INotificationRepository
{
    /// <summary>
    /// Retorna todas as notificações do usuário, ordenadas por data decrescente.
    /// </summary>
    Task<IEnumerable<Notification>> GetByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retorna uma notificação pelo Id, validando que pertence ao usuário.
    /// </summary>
    Task<Notification?> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retorna a contagem de notificações não lidas do usuário.
    /// </summary>
    Task<int> GetUnreadCountAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Notification notification,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Notification notification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marca todas as notificações do usuário como lidas.
    /// </summary>
    Task MarkAllAsReadAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
