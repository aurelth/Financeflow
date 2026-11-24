using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace FinanceFlow.Application.Services.Ai;

/// <param name="Limit">Chamadas permitidas por dia.</param>
/// <param name="Used">Chamadas já consumidas hoje.</param>
/// <param name="ResetsAtUtc">Próxima meia-noite do usuário, em UTC.</param>
public record AiQuotaStatus(int Limit, int Used, DateTime ResetsAtUtc)
{
    public int Remaining => Math.Max(0, Limit - Used);
}

/// <summary>Chamada reservada; devolva com <see cref="IAiQuotaService.ReleaseAsync"/> se a IA falhar.</summary>
public record AiQuotaLease(Guid UserId, DateOnly Day);

public interface IAiQuotaService
{
    Task<AiQuotaStatus> GetStatusAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Reserva uma chamada ou lança <see cref="QuotaExceededException"/>.</summary>
    Task<AiQuotaLease> AcquireAsync(Guid userId, CancellationToken cancellationToken = default);

    Task ReleaseAsync(AiQuotaLease lease, CancellationToken cancellationToken = default);
}

public class AiQuotaService(
    IAiUsageRepository usageRepository,
    IUserRepository userRepository,
    IOptions<AiOptions> options,
    TimeProvider clock) : IAiQuotaService
{
    public async Task<AiQuotaStatus> GetStatusAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var (day, resetsAtUtc) = await CurrentDayAsync(userId, cancellationToken);
        var used = await usageRepository.GetCountAsync(userId, day, cancellationToken);
        return new AiQuotaStatus(options.Value.DailyLimit, used, resetsAtUtc);
    }

    public async Task<AiQuotaLease> AcquireAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var limit = options.Value.DailyLimit;
        var (day, resetsAtUtc) = await CurrentDayAsync(userId, cancellationToken);

        if (limit <= 0 || !await usageRepository.TryConsumeAsync(userId, day, limit, cancellationToken))
        {
            throw new QuotaExceededException(
                limit <= 0
                    ? "O lançamento por IA não está disponível no momento."
                    : $"Você atingiu o limite de {limit} lançamentos por IA hoje. O limite volta amanhã.",
                limit,
                resetsAtUtc);
        }

        return new AiQuotaLease(userId, day);
    }

    public Task ReleaseAsync(AiQuotaLease lease, CancellationToken cancellationToken = default) =>
        usageRepository.ReleaseAsync(lease.UserId, lease.Day, cancellationToken);

    // O "dia" do limite é o do usuário: quem lança às 23h no Brasil não pode cair no dia seguinte em UTC
    private async Task<(DateOnly Day, DateTime ResetsAtUtc)> CurrentDayAsync(
        Guid userId, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        var zone = ResolveZone(user?.Timezone);

        var nowUtc = clock.GetUtcNow();
        var local = TimeZoneInfo.ConvertTime(nowUtc, zone);
        var day = DateOnly.FromDateTime(local.DateTime);

        var nextMidnightLocal = day.AddDays(1).ToDateTime(TimeOnly.MinValue);
        var resetsAtUtc = TimeZoneInfo.ConvertTimeToUtc(nextMidnightLocal, zone);

        return (day, resetsAtUtc);
    }

    // Fuso inválido ou ausente (imagem sem tzdata, valor antigo no banco) não pode derrubar o lançamento
    internal static TimeZoneInfo ResolveZone(string? timezoneId)
    {
        foreach (var id in new[] { timezoneId, "America/Sao_Paulo" })
        {
            if (string.IsNullOrWhiteSpace(id)) continue;
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException) { }
        }

        return TimeZoneInfo.Utc;
    }
}
