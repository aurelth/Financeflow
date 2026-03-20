using FluentValidation;

namespace FinanceFlow.Application.UseCases.Auth.Commands.RegisterUser;

public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("O nome é obrigatório.")
            .MinimumLength(2).WithMessage("O nome deve ter pelo menos 2 caracteres.")
            .MaximumLength(100).WithMessage("O nome deve ter no máximo 100 caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("O email é obrigatório.")
            .EmailAddress().WithMessage("O email informado não é válido.")
            .MaximumLength(200).WithMessage("O email deve ter no máximo 200 caracteres.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("A password é obrigatória.")
            .MinimumLength(8).WithMessage("A password deve ter pelo menos 8 caracteres.")
            .Matches("[A-Z]").WithMessage("A password deve conter pelo menos uma letra maiúscula.")
            .Matches("[0-9]").WithMessage("A password deve conter pelo menos um número.")
            .Matches("[^a-zA-Z0-9]").WithMessage("A password deve conter pelo menos um símbolo.");

        RuleFor(x => x.Currency)
            .MaximumLength(10).WithMessage("A moeda deve ter no máximo 10 caracteres.")
            .When(x => x.Currency is not null);

        RuleFor(x => x.Timezone)
            .MaximumLength(50).WithMessage("O fuso horário deve ter no máximo 50 caracteres.")
            .When(x => x.Timezone is not null);
    }
}
