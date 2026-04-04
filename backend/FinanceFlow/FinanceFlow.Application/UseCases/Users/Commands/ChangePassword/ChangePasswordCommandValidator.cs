using FluentValidation;
using FinanceFlow.Application.UseCases.Auth.Commands.ResetPassword;

namespace FinanceFlow.Application.UseCases.Users.Commands.ChangePassword;

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("A senha atual é obrigatória.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("A nova senha é obrigatória.")
            .MinimumLength(8).WithMessage("Mínimo 8 caracteres.")
            .Matches("[A-Z]").WithMessage("Deve conter pelo menos uma letra maiúscula.")
            .Matches("[0-9]").WithMessage("Deve conter pelo menos um número.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Deve conter pelo menos um símbolo.")
            .NotEqual(x => x.CurrentPassword)
            .WithMessage("A nova senha não pode ser igual à senha atual.");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Confirmação de senha é obrigatória.")
            .Equal(x => x.NewPassword).WithMessage("As senhas não coincidem.");
    }
}
