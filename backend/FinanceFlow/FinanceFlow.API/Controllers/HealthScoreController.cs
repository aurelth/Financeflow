using FinanceFlow.Application.DTOs.HealthScore;
using FinanceFlow.Application.UseCases.HealthScore.Queries.GetHealthScore;
using FinanceFlow.Application.UseCases.HealthScore.Queries.GetHealthScoreHistory;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceFlow.API.Controllers;

[Authorize]
public class HealthScoreController(IMediator mediator) : BaseController(mediator)
{
    /// <summary>Retorna o score de saúde financeira do mês informado.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(HealthScoreResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        [FromQuery] int? month,
        [FromQuery] int? year,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var query = new GetHealthScoreQuery(
            UserId: CurrentUserId,
            Month: month ?? now.Month,
            Year: year ?? now.Year);

        var result = await Mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>Retorna o histórico de scores dos últimos 6 meses.</summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(IEnumerable<HealthScoreHistoryItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory(CancellationToken cancellationToken)
    {
        var query = new GetHealthScoreHistoryQuery(CurrentUserId);
        var result = await Mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}
