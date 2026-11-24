using FinanceFlow.Application.Common.Interfaces;

namespace FinanceFlow.Application.Services;

public interface IDashboardCacheInvalidator
{
    /// <summary>Descarta o cache do dashboard dos meses das datas informadas.</summary>
    Task InvalidateAsync(Guid userId, IEnumerable<DateTime> dates, CancellationToken cancellationToken = default);
}

public class DashboardCacheInvalidator(ICacheService cache) : IDashboardCacheInvalidator
{
    public async Task InvalidateAsync(
        Guid userId,
        IEnumerable<DateTime> dates,
        CancellationToken cancellationToken = default)
    {
        var months = dates
            .Select(d => (d.Year, d.Month))
            .Distinct();

        foreach (var (year, month) in months)
        {
            var keys = new[]
            {
                $"dashboard:summary:{userId}:{year}:{month}",
                $"dashboard:balance-evolution:{userId}:{year}:{month}",
                $"dashboard:expenses-by-category:{userId}:{year}:{month}",
                $"dashboard:weekly-comparison:{userId}:{year}:{month}",
            };

            foreach (var key in keys)
                await cache.RemoveAsync(key, cancellationToken);
        }
    }
}
