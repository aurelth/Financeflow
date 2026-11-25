using FinanceFlow.Application.DTOs.WeeklySummary;
using FinanceFlow.Application.UseCases.WeeklySummary.Commands.SendWeeklySummaryEmail;
using FinanceFlow.Application.UseCases.WeeklySummary.Queries.GetWeeklySummaryCandidates;
using FinanceFlow.Application.UseCases.WeeklySummary.Queries.GetWeeklySummaryNotification;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceFlow.API.Controllers;

/// <summary>
/// Resumo semanal proativo. Todos os endpoints são do Worker e exigem o JWT de serviço:
/// aceitam o id do usuário na rota, então um usuário comum jamais pode chamá-los.
/// </summary>
[Route("api/weekly-summary")]
[Authorize(Policy = "RequireService")]
public class WeeklySummaryController(IMediator mediator) : BaseController(mediator)
{
    /// <summary>Usuários para quem o resumo da semana está na hora de sair (segunda, a partir das 8h no fuso deles).</summary>
    [HttpGet("internal/candidates")]
    [ProducesResponseType(typeof(IEnumerable<WeeklySummaryCandidateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCandidates(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetWeeklySummaryCandidatesQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>Texto da notificação do resumo; 204 quando a semana não teve nada a contar.</summary>
    [HttpGet("internal/{userId:guid}/notification")]
    [ProducesResponseType(typeof(WeeklySummaryNotificationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetNotification(Guid userId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetWeeklySummaryNotificationQuery(userId), cancellationToken);
        return result is null ? NoContent() : Ok(result);
    }

    /// <summary>Envia o resumo por e-mail (não envia nada quando a semana não teve o que contar).</summary>
    [HttpPost("internal/{userId:guid}/email")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendEmail(Guid userId, CancellationToken cancellationToken)
    {
        await Mediator.Send(new SendWeeklySummaryEmailCommand(userId), cancellationToken);
        return NoContent();
    }
}
