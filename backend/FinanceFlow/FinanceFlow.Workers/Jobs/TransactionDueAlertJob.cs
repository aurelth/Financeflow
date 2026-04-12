using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FinanceFlow.Workers.Models;
using FinanceFlow.Workers.Services;
using Quartz;

namespace FinanceFlow.Workers.Jobs;

[DisallowConcurrentExecution]
public class TransactionDueAlertJob(
    NotificationDispatchService notificationDispatchService,
    NotificationDeduplicationService deduplicationService,
    ApiAuthService authService,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<TransactionDueAlertJob> logger) : IJob
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly int[] AlertDaysAhead = [1, 3];

    public async Task Execute(IJobExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;
        var today = DateTime.UtcNow.Date;

        logger.LogInformation(
            "TransactionDueAlertJob iniciado — verificando vencimentos para {Date}",
            today.ToString("dd/MM/yyyy"));

        foreach (var daysAhead in AlertDaysAhead)
        {
            var targetDate = today.AddDays(daysAhead);
            await ProcessDueDateAsync(targetDate, daysAhead, cancellationToken);
        }
    }

    private async Task ProcessDueDateAsync(
        DateTime targetDate,
        int daysAhead,
        CancellationToken cancellationToken)
    {
        var token = await authService.GetTokenAsync(cancellationToken);
        var client = httpClientFactory.CreateClient("FinanceFlowApi");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync(
            $"api/transactions/internal/due?targetDate={targetDate:yyyy-MM-dd}",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Erro ao obter transações com vencimento em {Date}: {Status}",
                targetDate.ToString("dd/MM/yyyy"), response.StatusCode);
            return;
        }

        var transactions = await response.Content
            .ReadFromJsonAsync<IEnumerable<DueTransactionResponse>>(
                JsonOptions, cancellationToken)
            ?? [];

        var list = transactions.ToList();

        // Filtra apenas despesas
        var despesas = list
            .Where(t => t.Type == "Expense")
            .ToList();

        logger.LogInformation(
            "{Count} despesa(s) com vencimento em {Date} ({Days} dia(s))",
            despesas.Count, targetDate.ToString("dd/MM/yyyy"), daysAhead);

        foreach (var transaction in despesas)
        {
            var type = daysAhead == 1 ? "TransactionDueTomorrow" : "TransactionDueIn3Days";

            // Respeita preferências de notificação do utilizador
            var preferenceEnabled = daysAhead == 1
                ? transaction.NotifyDueTomorrow
                : transaction.NotifyDueIn3Days;

            if (!preferenceEnabled)
            {
                logger.LogDebug(
                    "Notificação [{Type}] desativada pelo utilizador {UserId} — ignorando.",
                    type, transaction.UserId);
                continue;
            }

            var jaEnviada = await deduplicationService.AlreadySentTodayAsync(
                transaction.Id, type, cancellationToken);

            if (jaEnviada)
            {
                logger.LogDebug(
                    "Notificação [{Type}] já enviada hoje para TransactionId {Id} — ignorando.",
                    type, transaction.Id);
                continue;
            }

            var culture = new CultureInfo("pt-BR");
            var amountFormatted = transaction.Amount.ToString("C", culture);

            var message = daysAhead == 1
                ? $"⏰ A transação '{transaction.Description}' vence amanhã ({targetDate:dd/MM/yyyy}) — {amountFormatted}."
                : $"📅 A transação '{transaction.Description}' vence em 3 dias ({targetDate:dd/MM/yyyy}) — {amountFormatted}.";

            var notification = new NotificationEvent(
                UserId: transaction.UserId,
                Message: message,
                Type: type,
                CreatedAt: DateTime.UtcNow,
                ReferenceId: transaction.Id);

            await notificationDispatchService.ProcessAsync(notification, cancellationToken);

            await deduplicationService.MarkAsSentAsync(
                transaction.Id, type, cancellationToken);
        }
    }
}
