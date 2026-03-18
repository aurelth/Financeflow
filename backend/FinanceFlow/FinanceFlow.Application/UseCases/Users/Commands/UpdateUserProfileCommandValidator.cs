using FluentValidation;

namespace FinanceFlow.Application.UseCases.Users.Commands.UpdateUserProfile;

public class UpdateUserProfileCommandValidator
    : AbstractValidator<UpdateUserProfileCommand>
{
    public UpdateUserProfileCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("O nome é obrigatório.")
            .MinimumLength(2).WithMessage("O nome deve ter pelo menos 2 caracteres.")
            .MaximumLength(100).WithMessage("O nome deve ter no máximo 100 caracteres.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("A moeda é obrigatória.")
            .MaximumLength(10).WithMessage("A moeda deve ter no máximo 10 caracteres.");

        RuleFor(x => x.Timezone)
            .NotEmpty().WithMessage("O fuso horário é obrigatório.")
            .MaximumLength(50).WithMessage("O fuso horário deve ter no máximo 50 caracteres.");
    }
}
