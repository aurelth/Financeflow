using FinanceFlow.Application.Common.Interfaces;

namespace FinanceFlow.IntegrationTests;

public class NoOpEventPublisher : IEventPublisher
{
    public Task PublishAsync<T>(
        string topic,
        T message,
        CancellationToken cancellationToken = default)
        where T : class
        => Task.CompletedTask;
}
