using FinanceFlow.Domain.Entities;
using FluentValidation;

namespace FinanceFlow.Application.UseCases.Categories.Commands.CreateCategory;

public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("O nome é obrigatório.")
            .MinimumLength(2).WithMessage("O nome deve ter pelo menos 2 caracteres.")
            .MaximumLength(100).WithMessage("O nome deve ter no máximo 100 caracteres.");

        RuleFor(x => x.Icon)
            .NotEmpty().WithMessage("O ícone é obrigatório.")
            .MaximumLength(50).WithMessage("O ícone deve ter no máximo 50 caracteres.");

        RuleFor(x => x.Color)
            .NotEmpty().WithMessage("A cor é obrigatória.")
            .Matches("^#([A-Fa-f0-9]{6})$")
            .WithMessage("A cor deve ser um valor hexadecimal válido (ex: #6366f1).");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("O tipo deve ser Receita ou Despesa.");
    }
}
