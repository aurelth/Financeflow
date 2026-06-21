using FluentValidation;

namespace FinanceFlow.Application.UseCases.Users.Commands.UploadAvatar;

public class UploadAvatarCommandValidator : AbstractValidator<UploadAvatarCommand>
{
    private static readonly string[] AllowedTypes = ["image/jpeg", "image/png", "image/webp"];

    public UploadAvatarCommandValidator()
    {
        RuleFor(x => x.ContentType)
            .Must(ct => AllowedTypes.Contains(ct))
            .WithMessage("Apenas imagens JPEG, PNG ou WebP são permitidas.");

        RuleFor(x => x.FileStream)
            .Must(s => s.Length <= 5 * 1024 * 1024)
            .WithMessage("O tamanho máximo do arquivo é 5MB.");
    }
}
