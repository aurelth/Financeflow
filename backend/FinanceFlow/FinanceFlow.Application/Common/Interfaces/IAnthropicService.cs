namespace FinanceFlow.Application.Common.Interfaces;

public interface IAnthropicService
{
    Task<string> SendMessageAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default);
}
