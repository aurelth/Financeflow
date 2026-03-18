using FinanceFlow.Domain.Interfaces;
using StackExchange.Redis;

namespace FinanceFlow.Infrastructure.Auth;

public class RefreshTokenService(IConnectionMultiplexer redis) : IRefreshTokenService
{
    // TTL de 7 dias para o Refresh Token
    private static readonly TimeSpan Ttl = TimeSpan.FromDays(7);

    private static string Key(Guid userId) => $"refresh_token:{userId}";

    public async Task SaveAsync(
        Guid userId,
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var db = redis.GetDatabase();
        await db.StringSetAsync(Key(userId), refreshToken, Ttl);
    }

    public async Task<bool> ValidateAsync(
        Guid userId,
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var db = redis.GetDatabase();
        var stored = await db.StringGetAsync(Key(userId));

        return stored.HasValue && stored == refreshToken;
    }

    public async Task RevokeAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var db = redis.GetDatabase();
        await db.KeyDeleteAsync(Key(userId));
    }
}
