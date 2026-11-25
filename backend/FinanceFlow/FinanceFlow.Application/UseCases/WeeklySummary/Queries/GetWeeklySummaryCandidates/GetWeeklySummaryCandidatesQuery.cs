using FinanceFlow.Application.DTOs.WeeklySummary;
using MediatR;

namespace FinanceFlow.Application.UseCases.WeeklySummary.Queries.GetWeeklySummaryCandidates;

/// <summary>Usuários para quem o resumo da semana está na hora de sair agora (uso interno do Worker).</summary>
public record GetWeeklySummaryCandidatesQuery : IRequest<IReadOnlyList<WeeklySummaryCandidateDto>>;
