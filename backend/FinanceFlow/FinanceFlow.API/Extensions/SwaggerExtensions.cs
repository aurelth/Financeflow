using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;

namespace FinanceFlow.API.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerServices(
        this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "FinanceFlow API",
                Version = "v1",
                Description = "API de gestão financeira pessoal"
            });

            // Habilita autenticação JWT no Swagger
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Insira o token JWT: Bearer {seu_token}"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id   = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }
}
