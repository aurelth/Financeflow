using FinanceFlow.Application.DTOs.Imports;
using FluentValidation;

namespace FinanceFlow.Application.UseCases.Imports.Commands.UploadCsv;

public class UploadCsvCommandValidator : AbstractValidator<UploadCsvCommand>
{
    public UploadCsvCommandValidator()
    {
        RuleFor(x => x.FileName).NotEmpty();

        RuleFor(x => x.Mapping).NotNull().WithMessage("Informe o mapeamento das colunas.");

        When(x => x.Mapping is not null, () =>
        {
            RuleFor(x => x.Mapping.Delimiter)
                .Must(d => d is ";" or "," or "\t" or "|")
                .WithMessage("Separador inválido.");

            RuleFor(x => x.Mapping.DateFormat)
                .Must(f => CsvDateFormats.All.Contains(f))
                .WithMessage("Formato de data inválido.");

            RuleFor(x => x.Mapping.DecimalSeparator)
                .Must(s => s is "," or ".")
                .WithMessage("Separador decimal inválido.");

            RuleFor(x => x.Mapping.DateColumn).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Mapping.DescriptionColumn).GreaterThanOrEqualTo(0);

            RuleFor(x => x.Mapping)
                .Must(m => m.AmountColumn is >= 0 || m.DebitColumn is >= 0 || m.CreditColumn is >= 0)
                .WithMessage("Indique a coluna de valor, ou as colunas de débito e crédito.");
        });
    }
}
