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

        logger.LogInformation(
            "{Count} transação(ões) com vencimento em {Date} ({Days} dia(s))",
            list.Count, targetDate.ToString("dd/MM/yyyy"), daysAhead);

        foreach (var transaction in list)
        {
            // Formatação explícita em pt-BR
            var culture = new CultureInfo("pt-BR");
            var amountFormatted = transaction.Amount.ToString("C", culture);

            var type = daysAhead == 1 ? "TransactionDueTomorrow" : "TransactionDueIn3Days";
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
        }
    }
}
