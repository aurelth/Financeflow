using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Confluent.Kafka;
using FinanceFlow.Workers.Models;

namespace FinanceFlow.Workers.Services;

public class BudgetAlertService(
    IHttpClientFactory httpClientFactory,
    ApiAuthService authService,
    IConfiguration configuration,
    ILogger<BudgetAlertService> logger)
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task ProcessTransactionAsync(
        TransactionCreatedEvent transaction,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Processando transação {TransactionId} — Type: {Type}, UserId: {UserId}, CategoryId: {CategoryId}",
            transaction.TransactionId, transaction.Type, transaction.UserId, transaction.CategoryId);

        // Só processa despesas (Type == 2)
        if (transaction.Type != 2)
        {
            logger.LogInformation("Transação ignorada — não é despesa (Type={Type})", transaction.Type);
            return;
        }

        var now = transaction.Date;
        var summaries = await GetBudgetSummaryAsync(
            transaction.UserId, now.Month, now.Year, cancellationToken);

        var summaryList = summaries.ToList();

        logger.LogInformation(
            "Summary retornou {Count} orçamento(s) para UserId={UserId}, Mês={Month}, Ano={Year}",
            summaryList.Count, transaction.UserId, now.Month, now.Year);

        foreach (var s in summaryList)
        {
            logger.LogInformation(
                "  → CategoryId={CategoryId}, CategoryName={Name}, Percentage={Pct}%",
                s.CategoryId, s.CategoryName, s.Percentage);
        }

        var budgetForCategory = summaryList
            .FirstOrDefault(s => s.CategoryId == transaction.CategoryId);

        if (budgetForCategory is null)
        {
            logger.LogInformation(
                "Nenhum orçamento encontrado para CategoryId={CategoryId}",
                transaction.CategoryId);
            return;
        }

        logger.LogInformation(
            "Orçamento encontrado — CategoryName={Name}, Percentage={Pct}%",
            budgetForCategory.CategoryName, budgetForCategory.Percentage);

        await CheckAndPublishAlertAsync(budgetForCategory, transaction.UserId, cancellationToken);
    }

    private async Task<IEnumerable<BudgetSummaryResponse>> GetBudgetSummaryAsync(
        Guid userId,
        int month,
        int year,
        CancellationToken cancellationToken)
    {
        var token = await authService.GetTokenAsync(cancellationToken);
        var client = httpClientFactory.CreateClient("FinanceFlowApi");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync(
            $"api/budgets/internal/summary?userId={userId}&month={month}&year={year}",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Erro ao obter summary de orçamentos: {Status}", response.StatusCode);
            return [];
        }

        return await response.Content
            .ReadFromJsonAsync<IEnumerable<BudgetSummaryResponse>>(
                _jsonOptions, cancellationToken)
            ?? [];
    }

    private async Task CheckAndPublishAlertAsync(
        BudgetSummaryResponse summary,
        Guid userId,
        CancellationToken cancellationToken)
    {
        string? alertLevel = null;
        string? message = null;

        if (summary.Percentage >= 100)
        {
            alertLevel = "critical";
            message = $"⚠️ Orçamento de '{summary.CategoryName}' atingiu 100% do limite " +
                         $"({summary.SpentAmount:C} de {summary.LimitAmount:C}) em {summary.Month}/{summary.Year}.";
        }
        else if (summary.Percentage >= 80)
        {
            alertLevel = "warning";
            message = $"🔔 Orçamento de '{summary.CategoryName}' atingiu {summary.Percentage:F1}% do limite " +
                         $"({summary.SpentAmount:C} de {summary.LimitAmount:C}) em {summary.Month}/{summary.Year}.";
        }

        if (alertLevel is null)
            return;

        var alertTopic = configuration["Kafka:Topics:BudgetAlerts"] ?? "finance.budget.alerts";
        var notificationTopic = configuration["Kafka:Topics:NotificationsCreated"] ?? "finance.notifications.created";

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"]
        };

        using var producer = new ProducerBuilder<string, string>(producerConfig).Build();

        // Publica alerta de orçamento
        var alertEvent = new BudgetAlertEvent(
            UserId: userId,
            CategoryId: summary.CategoryId,
            CategoryName: summary.CategoryName,
            Month: summary.Month,
            Year: summary.Year,
            LimitAmount: summary.LimitAmount,
            SpentAmount: summary.SpentAmount,
            Percentage: summary.Percentage,
            AlertLevel: alertLevel,
            TriggeredAt: DateTime.UtcNow);

        await producer.ProduceAsync(
            alertTopic,
            new Message<string, string>
            {
                Key = userId.ToString(),
                Value = JsonSerializer.Serialize(alertEvent)
            },
            cancellationToken);

        // Publica notificação para o Worker de notificações
        var notificationEvent = new NotificationEvent(
            UserId: userId,
            Message: message,
            Type: alertLevel == "critical" ? "BudgetCritical" : "BudgetWarning",
            CreatedAt: DateTime.UtcNow);

        await producer.ProduceAsync(
            notificationTopic,
            new Message<string, string>
            {
                Key = userId.ToString(),
                Value = JsonSerializer.Serialize(notificationEvent)
            },
            cancellationToken);

        logger.LogInformation(
            "Alerta {Level} publicado para utilizador {UserId}, categoria {Category} ({Percentage:F1}%)",
            alertLevel, userId, summary.CategoryName, summary.Percentage);
    }
}
