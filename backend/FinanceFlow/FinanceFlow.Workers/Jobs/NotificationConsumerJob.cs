using System.Text.Json;
using Confluent.Kafka;
using FinanceFlow.Workers.Models;
using FinanceFlow.Workers.Services;
using Quartz;

namespace FinanceFlow.Workers.Jobs;

[DisallowConcurrentExecution]
public class NotificationConsumerJob(
    NotificationDispatchService notificationDispatchService,
    IConfiguration configuration,
    ILogger<NotificationConsumerJob> logger) : IJob
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task Execute(IJobExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;
        var bootstrapServers = configuration["Kafka:BootstrapServers"]!;
        var topic = configuration["Kafka:Topics:NotificationsCreated"]
                      ?? "finance.notifications.created";
        var groupId = configuration["Kafka:ConsumerGroupId:Notifications"]
                      ?? "notification-worker";

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        consumer.Subscribe(topic);

        logger.LogInformation("A consumir tópico '{Topic}'...", topic);

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
                    var notification = JsonSerializer.Deserialize<NotificationEvent>(
                        result.Message.Value, JsonOptions);

                    if (notification is not null)
                        await notificationDispatchService
                            .ProcessAsync(notification, cancellationToken);

                    consumer.Commit(result);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Erro ao processar notificação Kafka: {Message}",
                        result.Message.Value);
                }
            }
        }
        finally
        {
            consumer.Close();
        }
    }
}
