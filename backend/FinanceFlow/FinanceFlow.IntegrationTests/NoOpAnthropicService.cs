using FinanceFlow.Application.Common.Interfaces;

namespace FinanceFlow.IntegrationTests;

public class NoOpAnthropicService : IAnthropicService
{
    public Task<string> SendMessageAsync(
        string systemPrompt,
        string userMessage,
        CancellationToken cancellationToken = default)
        => Task.FromResult("Resposta simulada do assistente financeiro.");
}
