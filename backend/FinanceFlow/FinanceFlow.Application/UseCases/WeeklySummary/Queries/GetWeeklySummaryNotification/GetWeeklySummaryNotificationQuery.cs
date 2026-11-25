using FinanceFlow.Application.DTOs.WeeklySummary;
using MediatR;

namespace FinanceFlow.Application.UseCases.WeeklySummary.Queries.GetWeeklySummaryNotification;

/// <summary>Notificação do resumo semanal; nulo quando a semana não teve nada a contar.</summary>
public record GetWeeklySummaryNotificationQuery(Guid UserId) : IRequest<WeeklySummaryNotificationDto?>;
