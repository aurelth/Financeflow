using MediatR;

namespace FinanceFlow.Application.UseCases.WeeklySummary.Commands.SendWeeklySummaryEmail;

/// <summary>Envia o resumo semanal por e-mail (uso interno do Worker). Não faz nada se a semana não teve o que contar.</summary>
public record SendWeeklySummaryEmailCommand(Guid UserId) : IRequest;
