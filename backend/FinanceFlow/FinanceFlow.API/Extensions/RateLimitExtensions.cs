using AspNetCoreRateLimit;

namespace FinanceFlow.API.Extensions;

public static class RateLimitExtensions
{
    public static IServiceCollection AddRateLimitServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Necessário para o AspNetCoreRateLimit aceder ao HttpContext
        services.AddMemoryCache();
        services.AddHttpContextAccessor();

        // Carrega configuração do appsettings.json
        services.Configure<IpRateLimitOptions>(
            configuration.GetSection("IpRateLimiting"));

        // Armazena contadores em memória
        services.AddInMemoryRateLimiting();

        // Serviço de configuração do rate limit
        services.AddSingleton<IRateLimitConfiguration,
            RateLimitConfiguration>();

        return services;
    }
}
