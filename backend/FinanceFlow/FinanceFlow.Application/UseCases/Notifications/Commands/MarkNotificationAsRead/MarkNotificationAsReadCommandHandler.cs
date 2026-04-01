using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Notifications.Commands.MarkNotificationAsRead;

public class MarkNotificationAsReadCommandHandler(
    INotificationRepository notificationRepository
) : IRequestHandler<MarkNotificationAsReadCommand>
{
    public async Task Handle(
        MarkNotificationAsReadCommand request,
        CancellationToken cancellationToken)
    {
        var notification = await notificationRepository.GetByIdAsync(
            request.Id, request.UserId, cancellationToken)
            ?? throw new NotFoundException("Notificação não encontrada.");

        notification.IsRead = true;
        notification.UpdatedAt = DateTime.UtcNow;

        await notificationRepository.UpdateAsync(notification, cancellationToken);
    }
}
