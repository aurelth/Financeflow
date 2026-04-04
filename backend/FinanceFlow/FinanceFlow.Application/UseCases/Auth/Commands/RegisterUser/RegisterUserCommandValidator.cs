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

        RuleFor(x => x.Cpf)
            .NotEmpty().WithMessage("O CPF é obrigatório.")
            .Matches(@"^\d{3}\.\d{3}\.\d{3}-\d{2}$")
            .WithMessage("O CPF deve estar no formato 000.000.000-00.")
            .Must(BeValidCpf).WithMessage("O CPF informado não é válido.");

        RuleFor(x => x.Gender)
            .IsInEnum().WithMessage("O gênero informado não é válido.");

        RuleFor(x => x.Currency)
            .MaximumLength(10).WithMessage("A moeda deve ter no máximo 10 caracteres.")
            .When(x => x.Currency is not null);

        RuleFor(x => x.Timezone)
            .MaximumLength(50).WithMessage("O fuso horário deve ter no máximo 50 caracteres.")
            .When(x => x.Timezone is not null);
    }

    // Validação do dígito verificador do CPF
    private static bool BeValidCpf(string cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf)) return false;

        var digits = cpf.Replace(".", "").Replace("-", "");

        if (digits.Length != 11) return false;
        if (digits.Distinct().Count() == 1) return false;

        // Primeiro dígito verificador
        var sum = 0;
        for (var i = 0; i < 9; i++)
            sum += int.Parse(digits[i].ToString()) * (10 - i);
        var remainder = sum % 11;
        var firstDigit = remainder < 2 ? 0 : 11 - remainder;
        if (firstDigit != int.Parse(digits[9].ToString())) return false;

        // Segundo dígito verificador
        sum = 0;
        for (var i = 0; i < 10; i++)
            sum += int.Parse(digits[i].ToString()) * (11 - i);
        remainder = sum % 11;
        var secondDigit = remainder < 2 ? 0 : 11 - remainder;
        return secondDigit == int.Parse(digits[10].ToString());
    }
}
