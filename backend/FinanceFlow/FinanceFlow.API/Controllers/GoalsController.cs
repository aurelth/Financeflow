using FinanceFlow.Application.DTOs.Goals;
using FinanceFlow.Application.UseCases.Goals.Commands.CreateGoal;
using FinanceFlow.Application.UseCases.Goals.Commands.DeleteGoal;
using FinanceFlow.Application.UseCases.Goals.Commands.UpdateGoal;
using FinanceFlow.Application.UseCases.Goals.Queries.GetGoalsSummary;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceFlow.API.Controllers;

[Authorize]
public class GoalsController(IMediator mediator) : BaseController(mediator)
{
    /// <summary>Retorna todas as metas com progresso calculado.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(GoalsSummaryResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var query = new GetGoalsSummaryQuery(CurrentUserId);
        var result = await Mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>Cria uma nova meta financeira.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(GoalProgressResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateGoalRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new CreateGoalCommand(
            UserId: CurrentUserId,
            Name: request.Name,
            TargetAmount: request.TargetAmount,
            MonthlyContribution: request.MonthlyContribution,
            Deadline: request.Deadline,
            Emoji: request.Emoji);

        var result = await Mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetAll), result);
    }

    /// <summary>Atualiza uma meta existente.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(GoalProgressResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateGoalRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateGoalCommand(
            Id: id,
            UserId: CurrentUserId,
            Name: request.Name,
            TargetAmount: request.TargetAmount,
            MonthlyContribution: request.MonthlyContribution,
            Deadline: request.Deadline,
            Emoji: request.Emoji);

        var result = await Mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>Remove uma meta (soft delete).</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new DeleteGoalCommand(id, CurrentUserId);
        await Mediator.Send(command, cancellationToken);
        return NoContent();
    }
}
