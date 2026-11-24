using FinanceFlow.Domain.Entities;
using FluentValidation;

namespace FinanceFlow.Application.UseCases.Recurring.Commands.DismissRecurringSuggestion;

public class DismissRecurringSuggestionCommandValidator : AbstractValidator<DismissRecurringSuggestionCommand>
{
    public DismissRecurringSuggestionCommandValidator()
    {
        RuleFor(c => c.Key)
            .NotEmpty().WithMessage("Informe a descrição da sugestão.")
            .MaximumLength(CategoryRule.MaxKeyLength).WithMessage("Descrição longa demais.");

        RuleFor(c => c.Type)
            .NotEqual(TransactionType.Transfer).WithMessage("Transferências não são recorrências sugeridas.");
    }
}
