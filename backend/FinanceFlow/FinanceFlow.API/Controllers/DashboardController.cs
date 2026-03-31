using FinanceFlow.Application.DTOs;
using FinanceFlow.Application.UseCases.Dashboard.Queries.GetBalanceEvolution;
using FinanceFlow.Application.UseCases.Dashboard.Queries.GetDashboardSummary;
using FinanceFlow.Application.UseCases.Dashboard.Queries.GetExpensesByCategory;
using FinanceFlow.Application.UseCases.Dashboard.Queries.GetPeriodComparison;
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

    /// <summary>
    /// Retorna o comparativo histórico entre até 3 períodos.
    /// Exemplo: GET /api/dashboard/period-comparison?periods=2026-01&periods=2026-02&periods=2026-03
    /// </summary>
    [HttpGet("period-comparison")]
    [ProducesResponseType(typeof(PeriodComparisonDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPeriodComparison(
        [FromQuery] string[] periods,
        CancellationToken cancellationToken)
    {
        if (periods.Length == 0 || periods.Length > 3)
            return BadRequest("Informe entre 1 e 3 períodos no formato YYYY-MM.");

        var parsedPeriods = new List<(int Month, int Year)>();

        foreach (var period in periods)
        {
            var parts = period.Split('-');
            if (parts.Length != 2 ||
                !int.TryParse(parts[0], out var year) ||
                !int.TryParse(parts[1], out var month) ||
                month < 1 || month > 12)
                return BadRequest($"Período inválido: '{period}'. Use o formato YYYY-MM.");

            parsedPeriods.Add((month, year));
        }

        var query = new GetPeriodComparisonQuery(CurrentUserId, parsedPeriods);
        var result = await Mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}
