using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Notifications.Commands.CreateNotification;

public class CreateNotificationCommandHandler(
    INotificationRepository notificationRepository
) : IRequestHandler<CreateNotificationCommand, Guid>
{
    public async Task<Guid> Handle(
        CreateNotificationCommand request,
        CancellationToken cancellationToken)
    {
        var notification = new Notification
        {
            UserId = request.UserId,
            Type = request.Type,
            Message = request.Message,
            IsRead = false
        };

        await notificationRepository.AddAsync(notification, cancellationToken);

        return notification.Id;
    }
}
