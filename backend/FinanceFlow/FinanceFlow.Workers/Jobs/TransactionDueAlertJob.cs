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
    ILogger<TransactionDueAlertJob> logger) : IJob
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Os avisos de vencimento. DaysAhead negativo olha para trás: -1 pega o que venceu ontem e ainda não foi pago,
    /// o que avisa uma única vez (amanhã o lançamento já não cai na janela de ontem).
    /// </summary>
    private sealed record Reminder(
        int DaysAhead,
        string Type,
        Func<DueTransactionResponse, bool> IsEnabled,
        Func<DueTransactionResponse, DateTime, string, string> BuildMessage);

    private static readonly Reminder[] Reminders =
    [
        new(0, "TransactionDueToday", t => t.NotifyDueToday,
            (t, date, amount) => $"🔔 A transação '{t.Description}' vence hoje ({date:dd/MM/yyyy}) — {amount}."),
        new(1, "TransactionDueTomorrow", t => t.NotifyDueTomorrow,
            (t, date, amount) => $"⏰ A transação '{t.Description}' vence amanhã ({date:dd/MM/yyyy}) — {amount}."),
        new(3, "TransactionDueIn3Days", t => t.NotifyDueIn3Days,
            (t, date, amount) => $"📅 A transação '{t.Description}' vence em 3 dias ({date:dd/MM/yyyy}) — {amount}."),
        new(-1, "TransactionOverdue", t => t.NotifyOverdue,
            (t, date, amount) => $"⚠️ A transação '{t.Description}' venceu ontem ({date:dd/MM/yyyy}) e ainda não foi paga — {amount}."),
    ];

    public async Task Execute(IJobExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;
        var today = DateTime.UtcNow.Date;

        logger.LogInformation(
            "TransactionDueAlertJob iniciado — verificando vencimentos para {Date}",
            today.ToString("dd/MM/yyyy"));

        foreach (var reminder in Reminders)
        {
            var targetDate = today.AddDays(reminder.DaysAhead);
            await ProcessDueDateAsync(targetDate, reminder, cancellationToken);
        }
    }

    private async Task ProcessDueDateAsync(
        DateTime targetDate,
        Reminder reminder,
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

        // A API só devolve o que ainda não foi pago; os lembretes são de contas a pagar (despesas)
        var despesas = transactions
            .Where(t => t.Type == "Expense")
            .ToList();

        logger.LogInformation(
            "{Count} despesa(s) em aberto com vencimento em {Date} [{Type}]",
            despesas.Count, targetDate.ToString("dd/MM/yyyy"), reminder.Type);

        var culture = new CultureInfo("pt-BR");

        foreach (var transaction in despesas)
        {
            // Respeita preferências de notificação do utilizador
            if (!reminder.IsEnabled(transaction))
            {
                logger.LogDebug(
                    "Notificação [{Type}] desativada pelo utilizador {UserId} — ignorando.",
                    reminder.Type, transaction.UserId);
                continue;
            }

            var jaEnviada = await deduplicationService.AlreadySentTodayAsync(
                transaction.Id, reminder.Type, cancellationToken);

            if (jaEnviada)
            {
                logger.LogDebug(
                    "Notificação [{Type}] já enviada hoje para TransactionId {Id} — ignorando.",
                    reminder.Type, transaction.Id);
                continue;
            }

            var message = reminder.BuildMessage(
                transaction, targetDate, transaction.Amount.ToString("C", culture));

            var notification = new NotificationEvent(
                UserId: transaction.UserId,
                Message: message,
                Type: reminder.Type,
                CreatedAt: DateTime.UtcNow,
                ReferenceId: transaction.Id);

            await notificationDispatchService.ProcessAsync(notification, cancellationToken);

            await deduplicationService.MarkAsSentAsync(
                transaction.Id, reminder.Type, cancellationToken);
        }
    }
}
