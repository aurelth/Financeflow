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

    // Resumo semanal: uma vez por usuário e semana (a semana, não o dia UTC, define a chave — a segunda-feira local
    // de quem está no Brasil atravessa a meia-noite UTC). TTL de 8 dias cobre o intervalo até o próximo resumo.
    public async Task<bool> WeeklySummaryAlreadySentAsync(
        Guid userId,
        string channel,
        DateTime weekStart) =>
        await _db.KeyExistsAsync(BuildWeeklyKey(userId, channel, weekStart));

    public async Task MarkWeeklySummarySentAsync(
        Guid userId,
        string channel,
        DateTime weekStart)
    {
        await _db.StringSetAsync(BuildWeeklyKey(userId, channel, weekStart), "1", TimeSpan.FromDays(8));
        logger.LogDebug(
            "Resumo semanal [{Channel}] marcado como enviado para UserId {UserId} (semana de {WeekStart:yyyy-MM-dd})",
            channel, userId, weekStart);
    }

    private static string BuildWeeklyKey(Guid userId, string channel, DateTime weekStart) =>
        $"notif:weekly:{userId}:{channel}:{weekStart:yyyyMMdd}";

    // Chave: notif:due:{transactionId}:{notificationType}:{data-hoje}
    private static string BuildKey(Guid transactionId, string notificationType) =>
        $"notif:due:{transactionId}:{notificationType}:{DateTime.UtcNow:yyyyMMdd}";
}
