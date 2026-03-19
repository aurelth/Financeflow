using FinanceFlow.API.Extensions;
using FinanceFlow.API.Middlewares;
using FinanceFlow.Infrastructure;
using FinanceFlow.Application;
using FinanceFlow.Infrastructure.Persistence;
using FinanceFlow.Infrastructure.Persistence.Context;
using Serilog;
using AspNetCoreRateLimit;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration));

// Extensions
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApiServices(builder.Configuration)
    .AddSwaggerServices()
    .AddHealthCheckServices(builder.Configuration)
    .AddRateLimitServices(builder.Configuration);

var app = builder.Build();

// Middlewares
app.UseIpRateLimiting();
app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "FinanceFlow API v1");
    c.RoutePrefix = string.Empty; // Swagger to root: http://localhost:5000
});

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
app.MapHubs();

// Seed
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider
        .GetRequiredService<FinanceFlowDbContext>();
    var logger = scope.ServiceProvider
        .GetRequiredService<ILogger<Program>>();
    await DbSeeder.SeedAsync(context, logger);
}

app.Run();
