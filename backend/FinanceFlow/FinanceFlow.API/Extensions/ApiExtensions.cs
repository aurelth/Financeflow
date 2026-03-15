namespace FinanceFlow.API.Extensions;

public static class ApiExtensions
{
    public static IServiceCollection AddApiServices(
        this IServiceCollection services,
        IConfiguration? configuration = null)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddHttpContextAccessor();
        services.AddSignalR();

        return services;
    }

    public static WebApplication MapHubs(this WebApplication app)
    {
        // Hubs SignalR serão mapeados aqui.       
        return app;
    }
}
