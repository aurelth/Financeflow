using AutoMapper;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Notifications.Queries.GetNotifications;

public class GetNotificationsQueryHandler(
    INotificationRepository notificationRepository,
    IMapper mapper
) : IRequestHandler<GetNotificationsQuery, IEnumerable<NotificationDto>>
{
    public async Task<IEnumerable<NotificationDto>> Handle(
        GetNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        var notifications = await notificationRepository.GetByUserAsync(
            request.UserId, cancellationToken);

        return mapper.Map<IEnumerable<NotificationDto>>(notifications);
    }
}
