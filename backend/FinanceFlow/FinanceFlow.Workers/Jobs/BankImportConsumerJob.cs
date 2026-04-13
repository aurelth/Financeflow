using System.Text.Json;
using Confluent.Kafka;
using FinanceFlow.Workers.Models;
using FinanceFlow.Workers.Services;
using Quartz;

namespace FinanceFlow.Workers.Jobs;

[DisallowConcurrentExecution]
public class BankImportConsumerJob(
    BankImportProcessingService processingService,
    IConfiguration configuration,
    ILogger<BankImportConsumerJob> logger) : IJob
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task Execute(IJobExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;
        var bootstrapServers = configuration["Kafka:BootstrapServers"]!;
        var topic = configuration["Kafka:Topics:BankImportCreated"] ?? "finance.bankimport.created";
        var groupId = configuration["Kafka:ConsumerGroupId"] ?? "bankimport-worker";

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
        };

        using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        consumer.Subscribe(topic);

        logger.LogInformation("A consumir topic '{Topic}'...", topic);

        try
        {
            var deadline = DateTime.UtcNow.AddSeconds(10);

            while (DateTime.UtcNow < deadline && !cancellationToken.IsCancellationRequested)
            {
                var result = consumer.Consume(TimeSpan.FromSeconds(1));
                if (result is null)
                    continue;

                try
                {
                    var importEvent = JsonSerializer.Deserialize<BankImportCreatedEvent>(
                        result.Message.Value, JsonOptions);

                    if (importEvent is not null)
                        await processingService.ProcessAsync(importEvent, cancellationToken);

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
