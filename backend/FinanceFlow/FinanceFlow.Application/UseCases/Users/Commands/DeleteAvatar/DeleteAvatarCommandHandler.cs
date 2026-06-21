using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Users.Commands.DeleteAvatar;

public class DeleteAvatarCommandHandler(
    IUserRepository userRepository,
    IAvatarService avatarService)
    : IRequestHandler<DeleteAvatarCommand>
{
    public async Task Handle(
        DeleteAvatarCommand request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("Utilizador", request.UserId);

        if (!string.IsNullOrEmpty(user.AvatarPath))
        {
            avatarService.Delete(user.AvatarPath);
            user.AvatarPath = null;
            user.UpdatedAt = DateTime.UtcNow;
            await userRepository.UpdateAsync(user, cancellationToken);
        }
    }
}
