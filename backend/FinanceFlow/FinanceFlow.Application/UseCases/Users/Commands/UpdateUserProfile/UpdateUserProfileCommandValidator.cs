using FluentValidation;

namespace FinanceFlow.Application.UseCases.Users.Commands.UpdateUserProfile;

public class UpdateUserProfileCommandValidator
    : AbstractValidator<UpdateUserProfileCommand>
{
    public UpdateUserProfileCommandValidator()
    {      
        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("A moeda é obrigatória.")
            .MaximumLength(10).WithMessage("A moeda deve ter no máximo 10 caracteres.");

        RuleFor(x => x.Timezone)
            .NotEmpty().WithMessage("O fuso horário é obrigatório.")
            .MaximumLength(50).WithMessage("O fuso horário deve ter no máximo 50 caracteres.");

        RuleFor(x => x.Language)
            .NotEmpty().WithMessage("O idioma é obrigatório.")
            .Must(lang => new[] { "pt-BR", "en-US", "es-ES", "fr-FR" }.Contains(lang?.Trim()))
            .WithMessage("Idioma não suportado. Use: pt-BR, en-US, es-ES ou fr-FR.");
    }
}
