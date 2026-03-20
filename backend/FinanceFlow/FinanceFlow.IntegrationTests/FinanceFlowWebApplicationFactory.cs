using FinanceFlow.Infrastructure.Persistence.Context;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.Text;
using Testcontainers.MsSql;
using Testcontainers.Redis;

namespace FinanceFlow.IntegrationTests;

public class FinanceFlowWebApplicationFactory
    : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string TestJwtSecret = "integration-test-secret-key-min32chars!!";

    // Container do Redis para testes
    private readonly RedisContainer _redisContainer = new RedisBuilder()
        .WithImage("redis:7.2-alpine")
        .Build();

    // Container DO SQL Server para testes
    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("Test@12345!")
        .Build();

    public async Task InitializeAsync()
    {
        // Inicia ambos os containers em paralelo
        await Task.WhenAll(
            _sqlContainer.StartAsync(),
            _redisContainer.StartAsync());
    }

    public new async Task DisposeAsync()
    {        
        await Task.WhenAll(
            _sqlContainer.StopAsync(),
            _redisContainer.StopAsync());
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Injeta configurações antes do Program.cs rodar
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = TestJwtSecret,
                ["Jwt:Issuer"] = "FinanceFlow",
                ["Jwt:Audience"] = "FinanceFlowUsers",
                ["Jwt:ExpirationMinutes"] = "15",
                ["Redis:ConnectionString"] = "localhost:6379",
                ["Kafka:BootstrapServers"] = "localhost:9092",
                ["Kafka:GroupId"] = "financeflow-test-group",
                ["Storage:BasePath"] = "storage",
                ["ConnectionStrings:DefaultConnection"] = _sqlContainer.GetConnectionString()
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove APENAS o DbContext e reregistra com TestContainers
            var dbDescriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<FinanceFlowDbContext>));
            if (dbDescriptor != null)
                services.Remove(dbDescriptor);

            services.AddDbContext<FinanceFlowDbContext>(options =>
                options.UseSqlServer(_sqlContainer.GetConnectionString()));

            // Substitui o Redis pelo container de teste
            var redisDescriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(IConnectionMultiplexer));
            if (redisDescriptor != null)
                services.Remove(redisDescriptor);

            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(_redisContainer.GetConnectionString()));

            // Sobrescreve APENAS os parâmetros de validação do JWT
            services.PostConfigure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(TestJwtSecret)),
                        ValidateIssuer = true,
                        ValidIssuer = "FinanceFlow",
                        ValidateAudience = true,
                        ValidAudience = "FinanceFlowUsers",
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    };
                });

            // Aplica migrations
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider
                .GetRequiredService<FinanceFlowDbContext>();
            db.Database.Migrate();
        });

        builder.UseEnvironment("Testing");
    }
}
