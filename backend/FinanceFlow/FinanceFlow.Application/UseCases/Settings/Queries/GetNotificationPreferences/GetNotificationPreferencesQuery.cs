using FinanceFlow.Application.DTOs;
using MediatR;

namespace FinanceFlow.Application.UseCases.Settings.Queries.GetNotificationPreferences;

public record GetNotificationPreferencesQuery(Guid UserId) : IRequest<NotificationPreferencesDto>;
