using FinanceFlow.Application.DTOs;
using MediatR;

namespace FinanceFlow.Application.UseCases.Notifications.Queries.GetNotifications;

public record GetNotificationsQuery(
    Guid UserId
) : IRequest<IEnumerable<NotificationDto>>;
