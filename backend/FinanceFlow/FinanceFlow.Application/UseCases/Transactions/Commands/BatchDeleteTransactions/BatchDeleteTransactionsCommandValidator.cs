using FinanceFlow.Application.UseCases.Transactions.Commands.BatchUpdateTransactions;
using FluentValidation;

namespace FinanceFlow.Application.UseCases.Transactions.Commands.BatchDeleteTransactions;

public class BatchDeleteTransactionsCommandValidator : AbstractValidator<BatchDeleteTransactionsCommand>
{
    public BatchDeleteTransactionsCommandValidator()
    {
        RuleFor(x => x.Ids)
            .NotEmpty().WithMessage("Selecione ao menos um lançamento.")
            .Must(ids => ids.Distinct().Count() <= BatchUpdateTransactionsCommandValidator.MaxItems)
            .WithMessage($"Selecione no máximo {BatchUpdateTransactionsCommandValidator.MaxItems} lançamentos por vez.");
    }
}
