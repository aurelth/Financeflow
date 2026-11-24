using FinanceFlow.Application.DTOs.Calendar;
using MediatR;

namespace FinanceFlow.Application.UseCases.Calendar.Queries.GetCalendarMonth;

/// <summary>Contas a pagar e a receber do mês, dia a dia, mais as vencidas em aberto.</summary>
public record GetCalendarMonthQuery(Guid UserId, int Month, int Year) : IRequest<CalendarMonthDto>;
