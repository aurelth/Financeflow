using MediatR;

namespace FinanceFlow.Application.UseCases.Notifications.Commands.CreateNotification;

public record CreateNotificationCommand(
    Guid UserId,
    string Type,
    string Message
) : IRequest<Guid>;
