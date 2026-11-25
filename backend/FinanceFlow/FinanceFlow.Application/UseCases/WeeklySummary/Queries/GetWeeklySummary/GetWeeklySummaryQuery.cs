using FinanceFlow.Application.DTOs.WeeklySummary;
using MediatR;

namespace FinanceFlow.Application.UseCases.WeeklySummary.Queries.GetWeeklySummary;

/// <summary>Resumo da última semana fechada do usuário (segunda a domingo, no fuso dele).</summary>
public record GetWeeklySummaryQuery(Guid UserId) : IRequest<WeeklySummaryDto>;
