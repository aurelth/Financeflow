using FluentValidation;

namespace FinanceFlow.Application.UseCases.Reports.Commands.RequestReport;

public class RequestReportCommandValidator : AbstractValidator<RequestReportCommand>
{
    public RequestReportCommandValidator()
    {
        RuleFor(x => x.Month)
            .InclusiveBetween(1, 12).WithMessage("O mês deve ser entre 1 e 12.");

        RuleFor(x => x.Year)
            .GreaterThanOrEqualTo(2000).WithMessage("O ano é inválido.")
            .LessThanOrEqualTo(2100).WithMessage("O ano é inválido.");
    }
}
