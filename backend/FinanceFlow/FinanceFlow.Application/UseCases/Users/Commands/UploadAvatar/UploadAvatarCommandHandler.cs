using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Users.Commands.UploadAvatar;

public class UploadAvatarCommandHandler(
    IUserRepository userRepository,
    IAvatarService avatarService)
    : IRequestHandler<UploadAvatarCommand, UploadAvatarResponse>
{
    public async Task<UploadAvatarResponse> Handle(
        UploadAvatarCommand request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("Utilizador", request.UserId);

        // Remove avatar anterior se existir
        if (!string.IsNullOrEmpty(user.AvatarPath))
            avatarService.Delete(user.AvatarPath);

        // Salva e redimensiona o novo avatar
        var avatarPath = await avatarService.SaveAsync(
            request.FileStream,
            request.ContentType,
            request.UserId,
            cancellationToken);

        user.AvatarPath = avatarPath;
        user.UpdatedAt = DateTime.UtcNow;

        await userRepository.UpdateAsync(user, cancellationToken);

        return new UploadAvatarResponse(avatarService.GetUrl(avatarPath));
    }
}
