using FluentValidation;

namespace FinanceFlow.Application.UseCases.Budgets.Commands.UpdateBudget;

public class UpdateBudgetCommandValidator : AbstractValidator<UpdateBudgetCommand>
{
    public UpdateBudgetCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("O id do orçamento é obrigatório.");

        RuleFor(x => x.LimitAmount)
            .GreaterThan(0).WithMessage("O limite deve ser maior que zero.");
    }
}
