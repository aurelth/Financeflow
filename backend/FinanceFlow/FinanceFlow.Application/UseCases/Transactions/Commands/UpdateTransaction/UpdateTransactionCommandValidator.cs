using FluentValidation;

namespace FinanceFlow.Application.UseCases.Transactions.Commands.UpdateTransaction;

public class UpdateTransactionCommandValidator
    : AbstractValidator<UpdateTransactionCommand>
{
    public UpdateTransactionCommandValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("O valor deve ser maior que zero.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("O tipo deve ser Receita ou Despesa.");

        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("A data é obrigatória.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("A descrição deve ter no máximo 500 caracteres.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("O status deve ser Pago, Pendente ou Agendado.");

        RuleFor(x => x.RecurrenceType)
            .IsInEnum().WithMessage("O tipo de recorrência é inválido.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("A categoria é obrigatória.");
    }
}
