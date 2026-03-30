namespace FinanceFlow.Workers;

public class Worker(ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("FinanceFlow Worker iniciado em: {Time}", DateTimeOffset.Now);
        await Task.CompletedTask;
    }
}
