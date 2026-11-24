using FluentValidation;

namespace FinanceFlow.Application.UseCases.Calendar.Queries.GetCalendarMonth;

public class GetCalendarMonthQueryValidator : AbstractValidator<GetCalendarMonthQuery>
{
    public GetCalendarMonthQueryValidator()
    {
        RuleFor(q => q.Month).InclusiveBetween(1, 12).WithMessage("O mês deve estar entre 1 e 12.");
        RuleFor(q => q.Year).InclusiveBetween(2000, 2100).WithMessage("O ano deve estar entre 2000 e 2100.");
    }
}
