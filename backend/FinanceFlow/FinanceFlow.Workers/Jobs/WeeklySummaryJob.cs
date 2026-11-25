using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FinanceFlow.Workers.Models;
using FinanceFlow.Workers.Services;
using Quartz;

namespace FinanceFlow.Workers.Jobs;

/// <summary>
/// Resumo semanal proativo. Roda a cada hora porque "segunda-feira de manhã" é diferente para cada fuso:
/// a API diz quem está na janela agora e o Redis garante um envio por usuário e semana.
/// </summary>
[DisallowConcurrentExecution]
public class WeeklySummaryJob(
    NotificationDispatchService notificationDispatchService,
    NotificationDeduplicationService deduplicationService,
    ApiAuthService authService,
    IHttpClientFactory httpClientFactory,
    ILogger<WeeklySummaryJob> logger) : IJob
{
    internal const string NotificationType = "WeeklySummary";
    private const string InAppChannel = "inapp";
    private const string EmailChannel = "email";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task Execute(IJobExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;

        var token = await authService.GetTokenAsync(cancellationToken);
        var client = httpClientFactory.CreateClient("FinanceFlowApi");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("api/weekly-summary/internal/candidates", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Erro ao obter usuários do resumo semanal: {Status}", response.StatusCode);
            return;
        }

        var candidates = await response.Content
            .ReadFromJsonAsync<List<WeeklySummaryCandidateResponse>>(JsonOptions, cancellationToken) ?? [];

        if (candidates.Count == 0)
            return;

        logger.LogInformation("WeeklySummaryJob: {Count} usuário(s) na janela de envio", candidates.Count);

        foreach (var candidate in candidates)
        {
            try
            {
                if (candidate.NotifyInApp)
                    await SendInAppAsync(client, candidate, cancellationToken);

                if (candidate.NotifyEmail)
                    await SendEmailAsync(client, candidate, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Um usuário com problema não pode travar o resumo dos demais; a próxima hora tenta de novo
                logger.LogError(ex, "Erro ao enviar resumo semanal para UserId {UserId}", candidate.UserId);
            }
        }
    }

    private async Task SendInAppAsync(
        HttpClient client,
        WeeklySummaryCandidateResponse candidate,
        CancellationToken cancellationToken)
    {
        if (await deduplicationService.WeeklySummaryAlreadySentAsync(candidate.UserId, InAppChannel, candidate.WeekStart))
            return;

        var response = await client.GetAsync(
            $"api/weekly-summary/internal/{candidate.UserId}/notification", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            // Semana sem nada a contar: não há o que enviar, e não adianta perguntar de novo na próxima hora
            await deduplicationService.MarkWeeklySummarySentAsync(candidate.UserId, InAppChannel, candidate.WeekStart);
            return;
        }

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Erro ao montar resumo semanal de UserId {UserId}: {Status}", candidate.UserId, response.StatusCode);
            return;
        }

        var summary = await response.Content
            .ReadFromJsonAsync<WeeklySummaryNotificationResponse>(JsonOptions, cancellationToken);
        if (summary is null)
            return;

        var dispatched = await notificationDispatchService.ProcessAsync(
            new NotificationEvent(
                UserId: summary.UserId,
                Message: summary.Message,
                Type: NotificationType,
                CreatedAt: DateTime.UtcNow,
                ReferenceId: summary.ReferenceId),
            cancellationToken);

        // Só marca como enviado se a API aceitou: do contrário a próxima hora tenta de novo
        if (dispatched)
            await deduplicationService.MarkWeeklySummarySentAsync(candidate.UserId, InAppChannel, candidate.WeekStart);
    }

    private async Task SendEmailAsync(
        HttpClient client,
        WeeklySummaryCandidateResponse candidate,
        CancellationToken cancellationToken)
    {
        if (await deduplicationService.WeeklySummaryAlreadySentAsync(candidate.UserId, EmailChannel, candidate.WeekStart))
            return;

        var response = await client.PostAsync(
            $"api/weekly-summary/internal/{candidate.UserId}/email", content: null, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Erro ao enviar e-mail do resumo semanal de UserId {UserId}: {Status}",
                candidate.UserId, response.StatusCode);
            return;
        }

        await deduplicationService.MarkWeeklySummarySentAsync(candidate.UserId, EmailChannel, candidate.WeekStart);
    }
}
