using FluentValidation;

namespace FinanceFlow.Application.UseCases.Auth.Commands.LoginUser;

public class LoginUserCommandValidator : AbstractValidator<LoginUserCommand>
{
    public LoginUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("O email é obrigatório.")
            .EmailAddress().WithMessage("O email informado não é válido.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("A password é obrigatória.");
    }
}
