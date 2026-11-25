using FluentValidation;

namespace FinanceFlow.Application.UseCases.Transactions.Commands.BatchUpdateTransactions;

public class BatchUpdateTransactionsCommandValidator : AbstractValidator<BatchUpdateTransactionsCommand>
{
    /// <summary>Teto por requisição: mantém a operação rápida e o lote revisável na tela.</summary>
    public const int MaxItems = 200;

    public BatchUpdateTransactionsCommandValidator()
    {
        RuleFor(x => x.Ids)
            .NotEmpty().WithMessage("Selecione ao menos um lançamento.")
            .Must(ids => ids.Distinct().Count() <= MaxItems)
            .WithMessage($"Selecione no máximo {MaxItems} lançamentos por vez.");

        RuleFor(x => x)
            .Must(x => x.CategoryId.HasValue || x.AccountId.HasValue || x.Status.HasValue)
            .WithMessage("Informe ao menos uma alteração: categoria, conta ou status.")
            .OverridePropertyName("Changes");

        RuleFor(x => x.CategoryId)
            .NotEqual(Guid.Empty).When(x => x.CategoryId.HasValue)
            .WithMessage("Categoria inválida.");

        RuleFor(x => x.AccountId)
            .NotEqual(Guid.Empty).When(x => x.AccountId.HasValue)
            .WithMessage("Conta inválida.");

        RuleFor(x => x.Status)
            .IsInEnum().When(x => x.Status.HasValue)
            .WithMessage("O status deve ser Pago, Pendente ou Agendado.");
    }
}
