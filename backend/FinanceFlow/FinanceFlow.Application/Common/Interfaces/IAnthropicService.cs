namespace FinanceFlow.Application.Common.Interfaces;

/// <summary>Imagem enviada à IA junto com a pergunta (já normalizada, em memória).</summary>
public record AiImage(byte[] Data, string MediaType);

public interface IAnthropicService
{
    Task<string> SendMessageAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pergunta com imagens (comprovantes). Usa temperatura 0: é extração de dados, não conversa,
    /// e o mesmo comprovante deve render o mesmo resultado.
    /// </summary>
    Task<string> SendMessageWithImagesAsync(
        string systemPrompt,
        string userMessage,
        IReadOnlyList<AiImage> images,
        CancellationToken cancellationToken = default);
}
