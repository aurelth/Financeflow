using Confluent.Kafka;
using FinanceFlow.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FinanceFlow.Infrastructure.Messaging;

public class KafkaEventPublisher(
    IConfiguration configuration,
    ILogger<KafkaEventPublisher> logger)
    : IEventPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer = new ProducerBuilder<string, string>(
        new ProducerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"],
        }).Build();

    public async Task PublishAsync<T>(
        string topic,
        T message,
        CancellationToken cancellationToken = default)
        where T : class
    {
        try
        {
            var json = JsonSerializer.Serialize(message);

            await _producer.ProduceAsync(topic,
                new Message<string, string>
                {
                    Key = Guid.NewGuid().ToString(),
                    Value = json
                },
                cancellationToken);

            logger.LogInformation(
                "Evento publicado no tópico {Topic}: {Message}", topic, json);
        }
        catch (Exception ex)
        {
            // Não bloqueia a transação se o Kafka estiver indisponível
            logger.LogError(ex,
                "Erro ao publicar evento no tópico {Topic}", topic);
        }
    }

    public void Dispose() => _producer.Dispose();
}
