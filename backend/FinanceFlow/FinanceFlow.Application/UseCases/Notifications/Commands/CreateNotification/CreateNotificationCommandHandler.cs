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
        // Deduplicação — ignora se já existe notificação do mesmo tipo para a mesma referência no dia de hoje       
        if (request.ReferenceId.HasValue)
        {
            var alreadyExists = await notificationRepository.ExistsForTodayAsync(
                request.UserId,
                request.Type,
                request.ReferenceId.Value,
                cancellationToken);

            if (alreadyExists)
                return Guid.Empty;
        }

        var notification = new Notification
        {
            UserId = request.UserId,
            Type = request.Type,
            Message = request.Message,
            IsRead = false,
            ReferenceId = request.ReferenceId,
        };

        await notificationRepository.AddAsync(notification, cancellationToken);

        return notification.Id;
    }
}
