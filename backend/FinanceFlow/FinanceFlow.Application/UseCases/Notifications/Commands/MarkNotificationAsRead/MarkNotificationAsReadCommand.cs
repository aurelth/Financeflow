using MediatR;

namespace FinanceFlow.Application.UseCases.Notifications.Commands.MarkNotificationAsRead;

public record MarkNotificationAsReadCommand(
    Guid Id,
    Guid UserId
) : IRequest;
