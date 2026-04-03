using MediatR;

namespace FinanceFlow.Application.UseCases.Notifications.Commands.CreateNotification;

public record CreateNotificationCommand(
    Guid UserId,
    string Type,
    string Message,
    Guid? ReferenceId = null
) : IRequest<Guid>;
