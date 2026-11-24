
using FinanceFlow.Application.Common.Behaviors;
using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Application.Services;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace FinanceFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // AutoMapper
        services.AddAutoMapper(assembly);

        // MediatR
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);

            // Pipeline behaviors
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        // FluentValidation
        services.AddValidatorsFromAssembly(assembly);

        // Serviços de análise financeira
        services.AddScoped<IFinancialContextService, FinancialContextService>();
        services.AddScoped<IHealthScoreService, HealthScoreService>();
        services.AddScoped<IAvatarService, AvatarService>();

        // Importação de extratos
        services.AddScoped<IImportDeduplicationService, ImportDeduplicationService>();
        services.AddScoped<ITransferMatchingService, TransferMatchingService>();
        services.AddScoped<IDashboardCacheInvalidator, DashboardCacheInvalidator>();
        services.AddScoped<IAccountReconciliationService, AccountReconciliationService>();

        //Goals
        services.AddScoped<IGoalProgressService, GoalProgressService>();

        return services;
    }
}
