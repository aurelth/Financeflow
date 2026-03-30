using FinanceFlow.Application.DTOs;
using FinanceFlow.Application.UseCases.Dashboard.Queries.GetDashboardSummary;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceFlow.API.Controllers;

[Authorize]
public class DashboardController(IMediator mediator) : BaseController(mediator)
{
    /// <summary>Retorna o sumário financeiro do utilizador para um mês/ano.</summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(DashboardSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary(
        [FromQuery] int month,
        [FromQuery] int year,
        CancellationToken cancellationToken)
    {
        var query = new GetDashboardSummaryQuery(CurrentUserId, month, year);
        var result = await Mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}
