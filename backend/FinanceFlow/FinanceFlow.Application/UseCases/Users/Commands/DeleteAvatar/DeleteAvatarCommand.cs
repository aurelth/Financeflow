using MediatR;

namespace FinanceFlow.Application.UseCases.Users.Commands.DeleteAvatar;

public record DeleteAvatarCommand(Guid UserId) : IRequest;
