using FinanceFlow.API.Hubs;
using FinanceFlow.API.Services;
using FinanceFlow.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace FinanceFlow.API.Extensions;

public static class ApiExtensions
{
    public static IServiceCollection AddApiServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddHttpContextAccessor();
        services.AddSignalR();

        // Registro do serviço de hub de notificações
        services.AddScoped<INotificationHubService, NotificationHubService>();

        // CORS
        services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                policy
                    .WithOrigins(
                        "http://localhost:3000",
                        "https://localhost:3000")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        // JWT Bearer — Modificado: consolidado num único bloco com RoleClaimType
        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                var diagSecret = configuration["Jwt:Secret"];
                var diagIssuer = configuration["Jwt:Issuer"];
                var diagAud = configuration["Jwt:Audience"];

                Console.WriteLine($"=== DIAG JWT Secret length : {diagSecret?.Length ?? 0} ===");
                Console.WriteLine($"=== DIAG JWT Issuer        : {diagIssuer} ===");
                Console.WriteLine($"=== DIAG JWT Audience      : {diagAud} ===");

                var secret = configuration["Jwt:Secret"]
                    ?? throw new InvalidOperationException("Jwt:Secret não configurado.");
                var issuer = configuration["Jwt:Issuer"]
                    ?? throw new InvalidOperationException("Jwt:Issuer não configurado.");
                var audience = configuration["Jwt:Audience"]
                    ?? throw new InvalidOperationException("Jwt:Audience não configurado.");

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(secret)),
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    // Adicionado: mapeia a claim "role" corretamente
                    RoleClaimType = "role",
                };
            });

        // Policy de Admin
        services.AddAuthorization(options =>
        {
            options.AddPolicy("RequireAdmin", policy =>
                policy.RequireClaim("role", "Admin"));
        });

        return services;
    }

    public static WebApplication MapHubs(this WebApplication app)
    {
        app.MapHub<ReportHub>("/hubs/reports");
        app.MapHub<NotificationHub>("/hubs/notifications");
        return app;
    }
}
