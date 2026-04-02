namespace FinanceFlow.Application.Common.Interfaces;

public interface INotificationHubService
{
    /// <summary>
    /// Envia uma notificação em tempo real para um usuário específico.
    /// </summary>
    Task SendNotificationAsync(
        Guid userId,
        string type,
        string message,
        CancellationToken cancellationToken = default);
}
