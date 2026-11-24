using FinanceFlow.Application.DTOs.Calendar;
using FinanceFlow.Application.UseCases.Calendar.Queries.GetCalendarMonth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceFlow.API.Controllers;

/// <summary>Calendário de contas a pagar e a receber.</summary>
[Authorize]
public class CalendarController(IMediator mediator) : BaseController(mediator)
{
    /// <summary>Lançamentos do mês agrupados por dia, totais em aberto/liquidados e as contas vencidas não pagas.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(CalendarMonthDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GetMonth(
        [FromQuery] int month,
        [FromQuery] int year,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetCalendarMonthQuery(CurrentUserId, month, year), cancellationToken);
        return Ok(result);
    }
}
