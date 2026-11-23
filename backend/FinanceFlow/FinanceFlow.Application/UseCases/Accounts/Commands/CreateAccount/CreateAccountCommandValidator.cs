using FluentValidation;

namespace FinanceFlow.Application.UseCases.Accounts.Commands.CreateAccount;

public class CreateAccountCommandValidator : AbstractValidator<CreateAccountCommand>
{
    public CreateAccountCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("O nome da conta é obrigatório.")
            .MaximumLength(100).WithMessage("O nome não pode exceder 100 caracteres.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Tipo de conta inválido.");

        RuleFor(x => x.BankName)
            .MaximumLength(100).WithMessage("O nome do banco não pode exceder 100 caracteres.");

        RuleFor(x => x.Currency)
            .Length(3).When(x => !string.IsNullOrWhiteSpace(x.Currency))
            .WithMessage("A moeda deve ter 3 letras (ex.: BRL).");

        RuleFor(x => x.Color)
            .Matches("^#[0-9A-Fa-f]{6}$").When(x => !string.IsNullOrWhiteSpace(x.Color))
            .WithMessage("A cor deve estar no formato #RRGGBB.");

        RuleFor(x => x.Icon)
            .MaximumLength(50).WithMessage("O ícone não pode exceder 50 caracteres.");
    }
}
