using System.Text.Json;
using Confluent.Kafka;
using FinanceFlow.Workers.Models;
using FinanceFlow.Workers.Services;
using Quartz;

namespace FinanceFlow.Workers.Jobs;

[DisallowConcurrentExecution]
public class BudgetAlertConsumerJob(
    BudgetAlertService budgetAlertService,
    IConfiguration configuration,
    ILogger<BudgetAlertConsumerJob> logger) : IJob
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task Execute(IJobExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;
        var bootstrapServers = configuration["Kafka:BootstrapServers"]!;
        var topic = configuration["Kafka:Topics:TransactionCreated"] ?? "finance.transactions.created";
        var groupId = configuration["Kafka:ConsumerGroupId"] ?? "budget-alert-worker";

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        consumer.Subscribe(topic);

        logger.LogInformation("A consumir topic '{Topic}'...", topic);

        try
        {
            // Consome mensagens disponíveis durante 10 segundos
            var deadline = DateTime.UtcNow.AddSeconds(10);

            while (DateTime.UtcNow < deadline && !cancellationToken.IsCancellationRequested)
            {
                var result = consumer.Consume(TimeSpan.FromSeconds(1));

                if (result is null)
                    continue;

                try
                {
                    var transaction = JsonSerializer.Deserialize<TransactionCreatedEvent>(
                        result.Message.Value, JsonOptions);

                    if (transaction is not null)
                        await budgetAlertService.ProcessTransactionAsync(transaction, cancellationToken);

                    consumer.Commit(result);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Erro ao processar mensagem Kafka: {Message}", result.Message.Value);
                }
            }
        }
        finally
        {
            consumer.Close();
        }
    }
}
