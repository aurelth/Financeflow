using MediatR;

namespace FinanceFlow.Application.UseCases.Notifications.Commands.MarkAllNotificationsAsRead;

public record MarkAllNotificationsAsReadCommand(
    Guid UserId
) : IRequest;
