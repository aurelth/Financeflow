using FinanceFlow.Workers.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace FinanceFlow.Workers.Jobs;

[DisallowConcurrentExecution]
public class MonthlyReportJob(
    ApiAuthService authService,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<MonthlyReportJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;

        logger.LogInformation("MonthlyReportJob iniciado em {Time}", DateTimeOffset.Now);

        try
        {
            // Mês anterior — relatório do mês que acabou
            var now = DateTime.UtcNow;
            var prev = now.AddMonths(-1);
            var month = prev.Month;
            var year = prev.Year;

            // Busca todos os usuários via endpoint interno
            var token = await authService.GetTokenAsync(cancellationToken);
            var client = httpClientFactory.CreateClient("FinanceFlowApi");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var usersResponse = await client.GetAsync(
                "api/users/internal", cancellationToken);

            if (!usersResponse.IsSuccessStatusCode)
            {
                logger.LogWarning("Falha ao buscar usuários para relatório mensal: {Status}",
                    usersResponse.StatusCode);
                return;
            }

            var users = await usersResponse.Content
                .ReadFromJsonAsync<IEnumerable<Guid>>(
                    cancellationToken: cancellationToken) ?? [];

            foreach (var userId in users)
            {
                try
                {
                    var payload = JsonSerializer.Serialize(new { month, year });
                    var content = new StringContent(payload, Encoding.UTF8, "application/json");

                    // Solicita geração do relatório para cada usuário
                    var response = await client.PostAsync(
                        "api/reports/request-internal", content, cancellationToken);

                    if (!response.IsSuccessStatusCode)
                        logger.LogWarning(
                            "Falha ao solicitar relatório mensal para usuário {UserId}: {Status}",
                            userId, response.StatusCode);
                    else
                        logger.LogInformation(
                            "Relatório mensal solicitado para usuário {UserId} — {Month}/{Year}",
                            userId, month, year);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Erro ao solicitar relatório mensal para usuário {UserId}", userId);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro no MonthlyReportJob");
        }
    }
}
