using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Notifications.Commands.MarkAllNotificationsAsRead;

public class MarkAllNotificationsAsReadCommandHandler(
    INotificationRepository notificationRepository
) : IRequestHandler<MarkAllNotificationsAsReadCommand>
{
    public async Task Handle(
        MarkAllNotificationsAsReadCommand request,
        CancellationToken cancellationToken)
    {
        await notificationRepository.MarkAllAsReadAsync(request.UserId, cancellationToken);
    }
}
