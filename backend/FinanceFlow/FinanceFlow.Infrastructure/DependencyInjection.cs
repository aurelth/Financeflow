using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Domain.Interfaces;
using FinanceFlow.Infrastructure.Auth;
using FinanceFlow.Infrastructure.Caching;
using FinanceFlow.Infrastructure.Email;
using FinanceFlow.Infrastructure.Messaging;
using FinanceFlow.Infrastructure.Persistence.Context;
using FinanceFlow.Infrastructure.Persistence.Repositories;
using FinanceFlow.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace FinanceFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // EF Core + SQL Server
        services.AddDbContext<FinanceFlowDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(
                    typeof(FinanceFlowDbContext).Assembly.FullName)));

        // Redis
        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(
                configuration["Redis:ConnectionString"]!));

        // Repositórios
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ISubcategoryRepository, SubcategoryRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IBudgetRepository, BudgetRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();


        // Caching
        services.AddScoped<ICacheService, RedisCacheService>();

        // Serviços de Auth
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();

        // Serviços de Storage
        services.AddScoped<IAttachmentService, LocalAttachmentService>();

        // Email (SendGrid)
        services.AddScoped<IEmailService, SendGridEmailService>();

        // Messaging
        services.AddSingleton<IEventPublisher, KafkaEventPublisher>();

        return services;
    }
}
