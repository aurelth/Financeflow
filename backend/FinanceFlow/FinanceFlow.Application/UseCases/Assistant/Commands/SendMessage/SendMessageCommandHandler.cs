using FinanceFlow.Application.Common.Interfaces;
using MediatR;
using System.Text.RegularExpressions;

namespace FinanceFlow.Application.UseCases.Assistant.Commands.SendMessage;

public class SendMessageCommandHandler(
    IFinancialContextService financialContextService,
    IAnthropicService anthropicService)
    : IRequestHandler<SendMessageCommand, SendMessageResponse>
{
    private const string SystemPrompt =
    """
    Você é um assistente financeiro pessoal integrado ao sistema FinanceFlow.
    Você tem acesso aos dados financeiros reais do utilizador para o período solicitado.
    
    Regras:
    - Responda APENAS perguntas relacionadas a finanças pessoais, orçamentos, gastos e economia.
    - Se o utilizador perguntar algo fora do contexto financeiro, redirecione educadamente.
    - Use os dados fornecidos no contexto para embasar suas respostas.
    - Seja conciso, direto e accionável — evite respostas longas e genéricas.
    - Responda sempre em português brasileiro.
    - Use valores em reais (R$) e formatação brasileira.
    - Quando identificar problemas (gastos excessivos, orçamentos no limite), aponte com clareza.
    - Quando houver pontos positivos, reforce-os brevemente.
    - NÃO use Markdown na resposta — sem #, ##, ###, **, *, - para listas, ou --- para separadores.
    - Para separar seções use uma linha em branco.
    - Para listas use números (1. 2. 3.) ou letras simples sem símbolos especiais.
    - Você pode analisar qualquer mês passado ou o mês atual quando solicitado.
    """;
    
    private static readonly Dictionary<string, int> MonthMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["janeiro"] = 1,
        ["jan"] = 1,
        ["fevereiro"] = 2,
        ["fev"] = 2,
        ["março"] = 3,
        ["mar"] = 3,
        ["marco"] = 3,
        ["abril"] = 4,
        ["abr"] = 4,
        ["maio"] = 5,
        ["mai"] = 5,
        ["junho"] = 6,
        ["jun"] = 6,
        ["julho"] = 7,
        ["jul"] = 7,
        ["agosto"] = 8,
        ["ago"] = 8,
        ["setembro"] = 9,
        ["set"] = 9,
        ["outubro"] = 10,
        ["out"] = 10,
        ["novembro"] = 11,
        ["nov"] = 11,
        ["dezembro"] = 12,
        ["dez"] = 12,
    };

    public async Task<SendMessageResponse> Handle(
        SendMessageCommand request,
        CancellationToken cancellationToken)
    {        
        var (month, year) = ExtractMonthAndYear(request.Message);

        // Recolhe o contexto financeiro do período identificado
        var financialContext = await financialContextService
            .BuildContextAsync(request.UserId, month, year, cancellationToken);
        
        var userMessageWithContext =
            $"""
            {financialContext}
            
            === PERGUNTA DO UTILIZADOR ===
            {request.Message}
            """;
        
        var reply = await anthropicService
            .SendMessageAsync(SystemPrompt, userMessageWithContext, cancellationToken);

        return new SendMessageResponse(reply);
    }

    /// <summary>
    /// Extrai mês e ano da mensagem usando regex.
    /// Exemplos suportados: "em abril", "janeiro de 2025", "no mês passado", "março/2024"
    /// Se não encontrar, retorna o mês e ano atuais.
    /// </summary>
    private static (int Month, int Year) ExtractMonthAndYear(string message)
    {
        var now = DateTime.UtcNow;
        var defaultMonth = now.Month;
        var defaultYear = now.Year;

        // Trata "mês passado"
        if (Regex.IsMatch(message, @"\bmês\s+passado\b", RegexOptions.IgnoreCase))
        {
            var lastMonth = now.AddMonths(-1);
            return (lastMonth.Month, lastMonth.Year);
        }

        // Trata "mês atual" ou "este mês"
        if (Regex.IsMatch(message, @"\b(mês\s+atual|este\s+mês)\b", RegexOptions.IgnoreCase))
            return (defaultMonth, defaultYear);

        // Tenta encontrar "mês de YYYY" ou "mês/YYYY" ou apenas "mês"
        var monthPattern = string.Join("|", MonthMap.Keys.Select(Regex.Escape));
        var fullPattern = $@"\b({monthPattern})\b(?:\s+de\s+|\s*/\s*)?(\d{{4}})?";
        var match = Regex.Match(message, fullPattern, RegexOptions.IgnoreCase);

        if (match.Success)
        {
            var monthName = match.Groups[1].Value;
            var yearStr = match.Groups[2].Value;

            if (MonthMap.TryGetValue(monthName, out var month))
            {
                var year = yearStr.Length == 4 && int.TryParse(yearStr, out var parsedYear)
                    ? parsedYear
                    : defaultYear;

                return (month, year);
            }
        }

        return (defaultMonth, defaultYear);
    }
}
