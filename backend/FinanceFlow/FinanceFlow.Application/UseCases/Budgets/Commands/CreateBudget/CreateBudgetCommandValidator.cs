using FluentValidation;

namespace FinanceFlow.Application.UseCases.Budgets.Commands.CreateBudget;

public class CreateBudgetCommandValidator : AbstractValidator<CreateBudgetCommand>
{
    public CreateBudgetCommandValidator()
    {
        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("A categoria é obrigatória.");

        RuleFor(x => x.Month)
            .InclusiveBetween(1, 12).WithMessage("O mês deve ser entre 1 e 12.");

        RuleFor(x => x.Year)
            .GreaterThanOrEqualTo(2000).WithMessage("O ano é inválido.")
            .LessThanOrEqualTo(2100).WithMessage("O ano é inválido.");

        RuleFor(x => x.LimitAmount)
            .GreaterThan(0).WithMessage("O limite deve ser maior que zero.");
    }
}
