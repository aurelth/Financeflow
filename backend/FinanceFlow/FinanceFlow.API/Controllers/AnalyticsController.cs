using FinanceFlow.Application.DTOs.Reports;
using FinanceFlow.Application.UseCases.Analytics.Queries.GetAnnualSummary;
using FinanceFlow.Application.UseCases.Analytics.Queries.GetCashFlow;
using FinanceFlow.Application.UseCases.Analytics.Queries.GetProjections;
using FinanceFlow.Application.UseCases.Analytics.Queries.GetReportByCategory;
using FinanceFlow.Application.UseCases.Analytics.Queries.GetReportByTag;
using FinanceFlow.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceFlow.API.Controllers;

[Authorize]
public class AnalyticsController(IMediator mediator) : BaseController(mediator)
{
    /// <summary>Retorna o fluxo de caixa para um período personalizado.</summary>
    [HttpGet("cash-flow")]
    [ProducesResponseType(typeof(CashFlowDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetCashFlow(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] string groupBy = "month",
        CancellationToken cancellationToken = default)
    {
        if (from > to)
            return BadRequest("A data de início não pode ser posterior à data de fim.");

        if (groupBy != "day" && groupBy != "month")
            return BadRequest("O parâmetro groupBy deve ser 'day' ou 'month'.");

        if (groupBy == "day" && (to - from).TotalDays > 90)
            return BadRequest("O agrupamento por dia suporta no máximo 90 dias.");

        var query = new GetCashFlowQuery(CurrentUserId, from, to, groupBy);
        var result = await Mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>Retorna o resumo financeiro anual agrupado por mês.</summary>
    [HttpGet("annual-summary")]
    [ProducesResponseType(typeof(AnnualSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAnnualSummary(
        [FromQuery] int year,
        CancellationToken cancellationToken = default)
    {
        if (year < 2000 || year > DateTime.UtcNow.Year + 1)
            return BadRequest("Ano inválido.");

        var query = new GetAnnualSummaryQuery(CurrentUserId, year);
        var result = await Mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>Retorna a distribuição de transações por categoria.</summary>
    [HttpGet("by-category")]
    [ProducesResponseType(typeof(ReportByCategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByCategory(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] TransactionType? type = null,
        CancellationToken cancellationToken = default)
    {
        if (from > to)
            return BadRequest("A data de início não pode ser posterior à data de fim.");

        var query = new GetReportByCategoryQuery(CurrentUserId, from, to, type);
        var result = await Mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>Retorna a distribuição de transações por tag.</summary>
    [HttpGet("by-tag")]
    [ProducesResponseType(typeof(ReportByTagDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByTag(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        CancellationToken cancellationToken = default)
    {
        if (from > to)
            return BadRequest("A data de início não pode ser posterior à data de fim.");

        var query = new GetReportByTagQuery(CurrentUserId, from, to);
        var result = await Mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>Retorna projecções financeiras baseadas no histórico com tendência linear e sazonalidade.</summary>
    [HttpGet("projections")]
    [ProducesResponseType(typeof(ProjectionsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetProjections(
        [FromQuery] int monthsBack = 12,
        [FromQuery] int monthsAhead = 3,
        CancellationToken cancellationToken = default)
    {
        if (monthsBack < 3 || monthsBack > 24)
            return BadRequest("O parâmetro monthsBack deve estar entre 3 e 24.");

        if (monthsAhead < 1 || monthsAhead > 6)
            return BadRequest("O parâmetro monthsAhead deve estar entre 1 e 6.");

        var query = new GetProjectionsQuery(CurrentUserId, monthsBack, monthsAhead);
        var result = await Mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}
