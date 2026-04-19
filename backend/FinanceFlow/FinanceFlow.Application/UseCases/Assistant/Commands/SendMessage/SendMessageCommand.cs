using MediatR;

namespace FinanceFlow.Application.UseCases.Assistant.Commands.SendMessage;

public record SendMessageCommand(
    Guid UserId,
    string Message
) : IRequest<SendMessageResponse>;

public record SendMessageResponse(
    string Reply
);
