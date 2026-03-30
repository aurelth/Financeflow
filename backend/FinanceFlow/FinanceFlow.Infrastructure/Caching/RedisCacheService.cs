using System.Text.Json;
using FinanceFlow.Application.Common.Interfaces;
using StackExchange.Redis;

namespace FinanceFlow.Infrastructure.Caching;

public class RedisCacheService(IConnectionMultiplexer redis) : ICacheService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
    {
        var db = redis.GetDatabase();
        var cached = await db.StringGetAsync(key);

        if (cached.HasValue)
        {
            return JsonSerializer.Deserialize<T>(cached!, JsonOptions)!;
        }

        var value = await factory();
        var serialized = JsonSerializer.Serialize(value, JsonOptions);

        await db.StringSetAsync(key, serialized, ttl);

        return value;
    }

    public async Task RemoveAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        var db = redis.GetDatabase();
        await db.KeyDeleteAsync(key);
    }
}
