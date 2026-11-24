using FinanceFlow.Application.Services;
using FinanceFlow.Domain.Entities;
using FluentValidation;

namespace FinanceFlow.Application.UseCases.Transactions.Commands.CreateTransaction;

public class CreateTransactionCommandValidator
    : AbstractValidator<CreateTransactionCommand>
{
    public CreateTransactionCommandValidator()
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

        When(x => x.InstallmentCount.HasValue, () =>
        {
            RuleFor(x => x.InstallmentCount!.Value)
                .InclusiveBetween(InstallmentPlanner.MinInstallments, InstallmentPlanner.MaxInstallments)
                .WithName("InstallmentCount")
                .WithMessage($"O número de parcelas deve estar entre {InstallmentPlanner.MinInstallments} e {InstallmentPlanner.MaxInstallments}.");

            RuleFor(x => x.Type)
                .NotEqual(TransactionType.Transfer)
                .WithMessage("Transferências não podem ser parceladas.");

            RuleFor(x => x.IsRecurring)
                .Equal(false)
                .WithMessage("Uma compra parcelada não pode ser recorrente ao mesmo tempo.");

            // Cada parcela precisa valer pelo menos um centavo
            RuleFor(x => x)
                .Must(x => x.Amount >= x.InstallmentCount!.Value * 0.01m)
                .WithName("Amount")
                .WithMessage("O valor total é pequeno demais para o número de parcelas (mínimo de R$ 0,01 por parcela).");
        });
    }
}
