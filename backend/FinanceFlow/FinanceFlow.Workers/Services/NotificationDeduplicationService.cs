using StackExchange.Redis;

namespace FinanceFlow.Workers.Services;

// Serviço de deduplicação de notificações via Redis
public class NotificationDeduplicationService(
    IConnectionMultiplexer redis,
    ILogger<NotificationDeduplicationService> logger)
{
    private readonly IDatabase _db = redis.GetDatabase();

    // Retorna true se a notificação já foi enviada hoje para esta transação
    public async Task<bool> AlreadySentTodayAsync(
        Guid transactionId,
        string notificationType,
        CancellationToken cancellationToken = default)
    {
        var key = BuildKey(transactionId, notificationType);
        return await _db.KeyExistsAsync(key);
    }

    // Marca a notificação como enviada com TTL de 24h
    public async Task MarkAsSentAsync(
        Guid transactionId,
        string notificationType,
        CancellationToken cancellationToken = default)
    {
        var key = BuildKey(transactionId, notificationType);
        await _db.StringSetAsync(key, "1", TimeSpan.FromHours(24));
        logger.LogDebug(
            "Notificação [{Type}] marcada como enviada para TransactionId {TransactionId}",
            notificationType, transactionId);
    }

    // Chave: notif:due:{transactionId}:{notificationType}:{data-hoje}
    private static string BuildKey(Guid transactionId, string notificationType) =>
        $"notif:due:{transactionId}:{notificationType}:{DateTime.UtcNow:yyyyMMdd}";
}
