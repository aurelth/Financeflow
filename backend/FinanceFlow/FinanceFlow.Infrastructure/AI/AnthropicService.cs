using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using FinanceFlow.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FinanceFlow.Infrastructure.AI;

public class AnthropicService : IAnthropicService
{
    private readonly AnthropicClient _client;
    private readonly string _model;
    private readonly int _maxTokens;
    private readonly ILogger<AnthropicService> _logger;

    public AnthropicService(IConfiguration configuration, ILogger<AnthropicService> logger)
    {
        _logger = logger;
        var apiKey = configuration["Anthropic:ApiKey"]
            ?? throw new InvalidOperationException("Anthropic:ApiKey não configurada.");
        _model = configuration["Anthropic:Model"] ?? "claude-sonnet-4-20250514";
        _maxTokens = int.TryParse(configuration["Anthropic:MaxTokens"], out var mt) ? mt : 1024;
        _client = new AnthropicClient(apiKey);
    }

    public async Task<string> SendMessageAsync(
        string systemPrompt,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Enviando mensagem para a API Anthropic.");

        var parameters = new MessageParameters
        {
            Model = _model,
            MaxTokens = _maxTokens,
            System = [new SystemMessage(systemPrompt)],
            Messages =
            [
                new Message(RoleType.User, userMessage)
            ]
        };

        var response = await _client.Messages.GetClaudeMessageAsync(parameters, cancellationToken);

        var content = response.Content.FirstOrDefault()?.ToString() ?? string.Empty;

        _logger.LogInformation("Resposta recebida da API Anthropic.");

        return content;
    }
}
