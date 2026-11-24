using FinanceFlow.Application.Common.Interfaces;

namespace FinanceFlow.IntegrationTests;

/// <summary>
/// O que a "IA" responde nos testes. Um por fábrica (singleton), nunca estático:
/// classes de teste rodam em paralelo e não podem pisar na resposta umas das outras.
/// </summary>
public class AiReplyStub
{
    public string QuickEntryReply { get; set; } = "{\"entries\":[]}";

    /// <summary>Quando preenchido, a "IA" falha com essa exceção (simula a Anthropic fora do ar).</summary>
    public Exception? Failure { get; set; }

    public int Calls { get; set; }
}

public class NoOpAnthropicService(AiReplyStub stub) : IAnthropicService
{
    public Task<string> SendMessageAsync(
        string systemPrompt,
        string userMessage,
        CancellationToken cancellationToken = default)
        => Task.FromResult("Resposta simulada do assistente financeiro.");

    public Task<string> SendMessageWithImagesAsync(
        string systemPrompt,
        string userMessage,
        IReadOnlyList<AiImage> images,
        CancellationToken cancellationToken = default)
    {
        stub.Calls++;

        return stub.Failure is { } failure
            ? Task.FromException<string>(failure)
            : Task.FromResult(stub.QuickEntryReply);
    }
}
