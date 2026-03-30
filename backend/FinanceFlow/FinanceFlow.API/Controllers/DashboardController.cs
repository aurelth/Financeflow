using FinanceFlow.Application.DTOs;
using FinanceFlow.Application.UseCases.Dashboard.Queries.GetBalanceEvolution;
using FinanceFlow.Application.UseCases.Dashboard.Queries.GetDashboardSummary;
using FinanceFlow.Application.UseCases.Dashboard.Queries.GetExpensesByCategory;
using FinanceFlow.Application.UseCases.Dashboard.Queries.GetWeeklyComparison;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceFlow.API.Controllers;

[Authorize]
public class DashboardController(IMediator mediator) : BaseController(mediator)
{
    /// <summary>Retorna o sumário financeiro do usuário para um mês/ano.</summary>
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

    /// <summary>Retorna a evolução diária do saldo para um mês/ano.</summary>
    [HttpGet("balance-evolution")]
    [ProducesResponseType(typeof(IEnumerable<BalanceEvolutionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBalanceEvolution(
        [FromQuery] int month,
        [FromQuery] int year,
        CancellationToken cancellationToken)
    {
        var query = new GetBalanceEvolutionQuery(CurrentUserId, month, year);
        var result = await Mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>Retorna as despesas agrupadas por categoria para um mês/ano.</summary>
    [HttpGet("expenses-by-category")]
    [ProducesResponseType(typeof(IEnumerable<ExpensesByCategoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExpensesByCategory(
        [FromQuery] int month,
        [FromQuery] int year,
        CancellationToken cancellationToken)
    {
        var query = new GetExpensesByCategoryQuery(CurrentUserId, month, year);
        var result = await Mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>Retorna a comparação semanal de receitas vs despesas para um mês/ano.</summary>
    [HttpGet("weekly-comparison")]
    [ProducesResponseType(typeof(IEnumerable<WeeklyComparisonDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWeeklyComparison(
        [FromQuery] int month,
        [FromQuery] int year,
        CancellationToken cancellationToken)
    {
        var query = new GetWeeklyComparisonQuery(CurrentUserId, month, year);
        var result = await Mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}
