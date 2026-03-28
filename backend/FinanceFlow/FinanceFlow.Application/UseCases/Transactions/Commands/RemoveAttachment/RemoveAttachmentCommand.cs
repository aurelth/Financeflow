using MediatR;

namespace FinanceFlow.Application.UseCases.Transactions.Commands.RemoveAttachment;

public record RemoveAttachmentCommand(
    Guid Id,
    Guid UserId
) : IRequest;
