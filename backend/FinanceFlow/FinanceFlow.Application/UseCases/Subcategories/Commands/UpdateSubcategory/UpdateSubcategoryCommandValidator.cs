using FluentValidation;

namespace FinanceFlow.Application.UseCases.Subcategories.Commands.UpdateSubcategory;

public class UpdateSubcategoryCommandValidator
    : AbstractValidator<UpdateSubcategoryCommand>
{
    public UpdateSubcategoryCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("O nome é obrigatório.")
            .MinimumLength(2).WithMessage("O nome deve ter pelo menos 2 caracteres.")
            .MaximumLength(100).WithMessage("O nome deve ter no máximo 100 caracteres.");
    }
}
