using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace FinanceFlow.Infrastructure.Persistence.Context;

public class FinanceFlowDbContextFactory
    : IDesignTimeDbContextFactory<FinanceFlowDbContext>
{
    public FinanceFlowDbContext CreateDbContext(string[] args)
    {
        // Lê o appsettings.Development.json do projeto API em design time
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(
                Directory.GetCurrentDirectory(),
                "..", "FinanceFlow.API"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<FinanceFlowDbContext>();

        optionsBuilder.UseSqlServer(
            configuration.GetConnectionString("DefaultConnection"),
            sql => sql.MigrationsAssembly(
                typeof(FinanceFlowDbContext).Assembly.FullName));

        return new FinanceFlowDbContext(optionsBuilder.Options);
    }
}
