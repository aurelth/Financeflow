using FluentValidation;

namespace FinanceFlow.Application.UseCases.Goals.Commands.UpdateGoal;

public class UpdateGoalCommandValidator : AbstractValidator<UpdateGoalCommand>
{
    public UpdateGoalCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("O nome é obrigatório.")
            .MaximumLength(100).WithMessage("O nome não pode exceder 100 caracteres.");

        RuleFor(x => x.TargetAmount)
            .GreaterThan(0).WithMessage("O valor alvo deve ser maior que zero.");

        RuleFor(x => x.MonthlyContribution)
            .GreaterThan(0).WithMessage("A contribuição mensal deve ser maior que zero.")
            .LessThanOrEqualTo(x => x.TargetAmount)
            .WithMessage("A contribuição mensal não pode ser maior que o valor alvo.");

        RuleFor(x => x.Deadline)
            .GreaterThan(DateTime.UtcNow).WithMessage("O prazo deve ser uma data futura.");

        RuleFor(x => x.Emoji)
            .NotEmpty().WithMessage("Selecione um emoji para a meta.");
    }
}
