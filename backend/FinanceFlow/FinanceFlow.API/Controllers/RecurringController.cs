using FinanceFlow.Application.DTOs.Recurring;
using FinanceFlow.Application.UseCases.Recurring.Commands.ConfirmRecurringSuggestion;
using FinanceFlow.Application.UseCases.Recurring.Commands.DismissRecurringSuggestion;
using FinanceFlow.Application.UseCases.Recurring.Commands.ResetDismissedRecurring;
using FinanceFlow.Application.UseCases.Recurring.Queries.GetRecurringSuggestions;
using FinanceFlow.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceFlow.API.Controllers;

/// <summary>Detecção de assinaturas e contas recorrentes a partir do histórico do próprio usuário.</summary>
[Authorize]
public class RecurringController(IMediator mediator) : BaseController(mediator)
{
    /// <summary>Recorrências encontradas no histórico e ainda não confirmadas nem dispensadas.</summary>
    [HttpGet("suggestions")]
    [ProducesResponseType(typeof(RecurringSuggestionsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSuggestions(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetRecurringSuggestionsQuery(CurrentUserId), cancellationToken);
        return Ok(result);
    }

    /// <summary>Confirma a sugestão: o histórico vira uma série recorrente e as próximas ocorrências são agendadas.</summary>
    [HttpPost("suggestions/confirm")]
    [ProducesResponseType(typeof(ConfirmRecurringResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Confirm([FromBody] RecurringSuggestionRef request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(
            new ConfirmRecurringSuggestionCommand(CurrentUserId, request.Type, request.Key), cancellationToken);
        return Ok(result);
    }

    /// <summary>Dispensa a sugestão ("não é uma assinatura"): ela não volta a aparecer.</summary>
    [HttpPost("suggestions/dismiss")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Dismiss([FromBody] RecurringSuggestionRef request, CancellationToken cancellationToken)
    {
        await Mediator.Send(
            new DismissRecurringSuggestionCommand(CurrentUserId, request.Type, request.Key ?? string.Empty), cancellationToken);
        return NoContent();
    }

    /// <summary>Volta a oferecer as sugestões dispensadas.</summary>
    [HttpDelete("suggestions/dismissed")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetDismissed(CancellationToken cancellationToken)
    {
        var restored = await Mediator.Send(new ResetDismissedRecurringCommand(CurrentUserId), cancellationToken);
        return Ok(new { restored });
    }
}

/// <summary>Identifica uma sugestão: a descrição normalizada e o tipo (receita/despesa).</summary>
public record RecurringSuggestionRef(TransactionType Type, string Key);
