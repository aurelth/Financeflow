using FinanceFlow.Application.Common.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Assistant.Commands.SendMessage;

public class SendMessageCommandHandler(
    IFinancialContextService financialContextService,
    IAnthropicService anthropicService)
    : IRequestHandler<SendMessageCommand, SendMessageResponse>
{
    private const string SystemPrompt =
        """
        Você é um assistente financeiro pessoal integrado ao sistema FinanceFlow.
        Você tem acesso aos dados financeiros reais do utilizador para o mês atual.
        
        Regras:
        - Responda APENAS perguntas relacionadas a finanças pessoais, orçamentos, gastos e economia.
        - Se o utilizador perguntar algo fora do contexto financeiro, redirecione educadamente.
        - Use os dados fornecidos no contexto para embasar suas respostas.
        - Seja conciso, direto e accionável — evite respostas longas e genéricas.
        - Responda sempre em português brasileiro.
        - Use valores em reais (R$) e formatação brasileira.
        - Quando identificar problemas (gastos excessivos, orçamentos no limite), aponte com clareza.
        - Quando houver pontos positivos, reforce-os brevemente.
        """;

    public async Task<SendMessageResponse> Handle(
        SendMessageCommand request,
        CancellationToken cancellationToken)
    {
        // Recolhe o contexto financeiro do utilizador
        var financialContext = await financialContextService
            .BuildContextAsync(request.UserId, cancellationToken);

        // Monta a mensagem do utilizador com o contexto financeiro embutido
        var userMessageWithContext =
            $"""
            {financialContext}
            
            === PERGUNTA DO UTILIZADOR ===
            {request.Message}
            """;

        // Envia para a API Anthropic e obtém a resposta
        var reply = await anthropicService
            .SendMessageAsync(SystemPrompt, userMessageWithContext, cancellationToken);

        return new SendMessageResponse(reply);
    }
}
