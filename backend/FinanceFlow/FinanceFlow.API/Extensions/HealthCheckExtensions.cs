using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FinanceFlow.API.Extensions;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddHealthCheckServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddHealthChecks()
            .AddSqlServer(
                connectionString: configuration.GetConnectionString("DefaultConnection")!,
                name: "sql-server",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["db", "sql"])
            .AddRedis(
                redisConnectionString: configuration["Redis:ConnectionString"]!,
                name: "redis",
                failureStatus: HealthStatus.Degraded,
                tags: ["cache", "redis"]);

        return services;
    }
}
