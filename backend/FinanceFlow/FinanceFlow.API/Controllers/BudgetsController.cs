using FinanceFlow.Application.DTOs;
using FinanceFlow.Application.UseCases.Budgets.Commands.CreateBudget;
using FinanceFlow.Application.UseCases.Budgets.Commands.DeleteBudget;
using FinanceFlow.Application.UseCases.Budgets.Commands.UpdateBudget;
using FinanceFlow.Application.UseCases.Budgets.Queries.GetBudgets;
using FinanceFlow.Application.UseCases.Budgets.Queries.GetBudgetSummary;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceFlow.API.Controllers;

[Authorize]
public class BudgetsController(IMediator mediator) : BaseController(mediator)
{
    /// <summary>Lista os orçamentos do utilizador para um mês/ano.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<BudgetDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int month,
        [FromQuery] int year,
        CancellationToken cancellationToken)
    {
        var query = new GetBudgetsQuery(CurrentUserId, month, year);
        var result = await Mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>Retorna o resumo de orçamentos (gasto vs limite) para um mês/ano.</summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(IEnumerable<BudgetSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary(
        [FromQuery] int month,
        [FromQuery] int year,
        CancellationToken cancellationToken)
    {
        var query = new GetBudgetSummaryQuery(CurrentUserId, month, year);
        var result = await Mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>Cria um novo orçamento.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(BudgetDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateBudgetRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new CreateBudgetCommand(
            UserId: CurrentUserId,
            CategoryId: request.CategoryId,
            Month: request.Month,
            Year: request.Year,
            LimitAmount: request.LimitAmount);

        var result = await Mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { month = result.Month, year = result.Year }, result);
    }

    /// <summary>Atualiza o limite de um orçamento existente.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(BudgetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateBudgetRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateBudgetCommand(
            Id: id,
            UserId: CurrentUserId,
            LimitAmount: request.LimitAmount);

        var result = await Mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>Remove um orçamento.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new DeleteBudgetCommand(id, CurrentUserId);
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }
}
