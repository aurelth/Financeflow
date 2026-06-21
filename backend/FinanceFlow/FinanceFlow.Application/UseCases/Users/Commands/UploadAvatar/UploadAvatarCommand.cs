using MediatR;

namespace FinanceFlow.Application.UseCases.Users.Commands.UploadAvatar;

public record UploadAvatarCommand(
    Guid UserId,
    Stream FileStream,
    string ContentType
) : IRequest<UploadAvatarResponse>;

public record UploadAvatarResponse(string AvatarUrl);
