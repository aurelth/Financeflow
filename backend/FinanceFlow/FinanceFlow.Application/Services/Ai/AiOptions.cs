namespace FinanceFlow.Application.Services.Ai;

/// <summary>Seção "Ai" do appsettings (ou variáveis de ambiente Ai__DailyLimit etc.).</summary>
public class AiOptions
{
    public const string SectionName = "Ai";

    /// <summary>Quantas chamadas de IA por usuário e por dia. 0 desliga a função para todos.</summary>
    public int DailyLimit { get; set; } = 30;

    /// <summary>Tamanho máximo da imagem do comprovante, em bytes (o limite da API da Anthropic é 5 MB).</summary>
    public int MaxImageBytes { get; set; } = 5 * 1024 * 1024;

    /// <summary>Tamanho máximo do texto livre, em caracteres.</summary>
    public int MaxTextLength { get; set; } = 500;

    /// <summary>Quantos lançamentos uma única chamada pode devolver.</summary>
    public int MaxDraftsPerRequest { get; set; } = 5;
}
